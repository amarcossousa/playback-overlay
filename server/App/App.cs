using System.Text.Json;
using NPSMLib;
using PlaybackDataServer.App.Extensions;
using PlaybackDataServer.App.Server;
using PlaybackDataServer.App.Structs;

namespace PlaybackDataServer.App
{
    public class App : IDisposable
    {
        private const string Address = "0.0.0.0";
        private const int Port = 9764;

        // Fallback de polling: cobre os casos em que a NPSM não dispara
        // nenhum evento ao trocar de faixa dentro do mesmo app (bug conhecido
        // da API subjacente, fora do nosso controle).
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

        private readonly Server.Server _server = new(Address, Port);
        private readonly NowPlayingSessionManager _npSessionManager = new();
        private readonly object _sessionLock = new();

        private NowPlayingSession? _npSession;
        private MediaPlaybackDataSource? _playback;
        private string _lastSentHash = string.Empty;

        // Qualificado explicitamente: System.Windows.Forms também define "Timer",
        // e com UseWindowsForms habilitado o compilador não resolve a ambiguidade.
        private System.Threading.Timer? _pollTimer;

        public void Start()
        {
            Console.WriteLine("Starting server...");
            _server.Start();

            Console.WriteLine("Starting Now Playing Session Manager...");
            ChangeSession(_npSessionManager.CurrentSession);

            _server.ClientConnected += OnServerClientConnected;
            _npSessionManager.SessionListChanged += OnNowPlayingSessionListChanged;

            _pollTimer = new System.Threading.Timer(_ => PollSession(), null, PollInterval, PollInterval);
        }

        public void Stop()
        {
            _pollTimer?.Dispose();
            _pollTimer = null;

            _server.ClientConnected -= OnServerClientConnected;
            _npSessionManager.SessionListChanged -= OnNowPlayingSessionListChanged;

            if (_playback is not null)
            {
                _playback.MediaPlaybackDataChanged -= OnPlaybackDataChanged;
            }

            Console.WriteLine("Stopping server...");
            _server.Stop();
        }

        public void Dispose()
        {
            Stop();
            _server.Dispose();
            GC.SuppressFinalize(this);
        }

        private void ChangeSession(NowPlayingSession session)
        {
            lock (_sessionLock)
            {
                if (_playback is not null)
                {
                    _playback.MediaPlaybackDataChanged -= OnPlaybackDataChanged;
                }

                _npSession = session;
                _playback = _npSession?.ActivateMediaPlaybackDataSource();

                if (_playback is not null)
                {
                    _playback.MediaPlaybackDataChanged += OnPlaybackDataChanged;
                }
            }

            SendMediaData();
        }

        private void PollSession()
        {
            try
            {
                var current = _npSessionManager.CurrentSession;

                lock (_sessionLock)
                {
                    if (_npSession is null || !Equals(current, _npSession))
                    {
                        ChangeSession(current);
                        return;
                    }
                }

                SendMediaData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while polling session: {ex.Message}");
            }
        }

        private void SendMediaData()
        {
            MediaPlaybackDataSource? playback;
            lock (_sessionLock)
            {
                playback = _playback;
            }

            if (playback is null)
            {
                Console.WriteLine("No active playback data source.");
                return;
            }

            try
            {
                var info = playback.GetMediaObjectInfo();
                var timeline = playback.GetMediaTimelineProperties();
                var state = playback.GetMediaPlaybackInfo();

                var progressLine =
                    $"Now playing: {info.Artist} - {info.Title} ({timeline.Position.Format()}/{timeline.EndTime.Format()})";

                var hash = $"{info.Artist}|{info.Title}|{timeline.EndTime}|{state.PlaybackState}";
                var isNewTrackOrState = hash != _lastSentHash;

                if (isNewTrackOrState)
                {
                    Console.WriteLine(progressLine);
                }

                if (_server.ConnectedSessions == 0)
                {
                    return;
                }

                string thumbnailBase64;
                try
                {
                    using var thumbnail = playback.GetThumbnailStream();
                    thumbnailBase64 = thumbnail != null ? thumbnail.ToBase64() : string.Empty;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to read thumbnail: {ex.Message}");
                    thumbnailBase64 = string.Empty;
                }

                var data = new MediaData
                {
                    Title = info.Title,
                    Artist = info.Artist,
                    Album = info.AlbumTitle,
                    Position = (int)timeline.Position.TotalSeconds,
                    Duration = (int)timeline.EndTime.TotalSeconds,
                    Thumbnail = thumbnailBase64,
                    IsPlaying = state.PlaybackState == MediaPlaybackState.Playing,
                    IsPaused = state.PlaybackState == MediaPlaybackState.Paused,
                    IsStopped = state.PlaybackState == MediaPlaybackState.Stopped
                };

                var json = JsonSerializer.Serialize(data);
                _lastSentHash = hash;

                if (isNewTrackOrState)
                {
                    Console.WriteLine($"Sending media data ({json.Length} bytes)...");
                }

                _server.MulticastText(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while sending media data: {ex}");
            }
        }

        private void OnNowPlayingSessionListChanged(object? sender, NowPlayingSessionManagerEventArgs e)
        {
            ChangeSession(_npSessionManager.CurrentSession);
        }

        private void OnServerClientConnected(object? sender, Session e)
        {
            SendMediaData();
        }

        private void OnPlaybackDataChanged(object? sender, MediaPlaybackDataChangedArgs e)
        {
            SendMediaData();
        }
    }
}
