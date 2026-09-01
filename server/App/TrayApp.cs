using System.Drawing;
using System.Windows.Forms;

namespace PlaybackDataServer.App
{
    public sealed class TrayApp : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly TrayMenuForm _menu;

        private App? _app;
        private bool _isRunning;

        public TrayApp()
        {
            _menu = new TrayMenuForm(isServerRunning: false);
            _menu.ToggleServerClicked += OnToggleServer;
            _menu.OpenLogsClicked += OnOpenLogs;
            _menu.ExitClicked += OnExit;

            _trayIcon = new NotifyIcon
            {
                Icon = LoadTrayIcon(),
                Text = "Playback Overlay Server (parado)",
                Visible = true
            };

            // ContextMenuStrip foi removido deliberadamente. O menu é um Form
            // próprio para ter hover, margem e layout totalmente controláveis.
            _trayIcon.MouseUp += OnTrayIconMouseUp;
            _trayIcon.DoubleClick += OnToggleServer;

            StartServer();
        }

        private static Icon LoadTrayIcon()
        {
            var iconPath = Path.Combine(
                AppContext.BaseDirectory,
                "Assets",
                "playback-overlay.ico");

            if (File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }

            return SystemIcons.Application;
        }

        private void OnTrayIconMouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right || e.Button == MouseButtons.Left)
            {
                ToggleMenu();
            }
        }

        private void ToggleMenu()
        {
            if (_menu.Visible)
            {
                _menu.Hide();
                return;
            }

            _menu.SetServerRunning(_isRunning);
            _menu.ShowNearCursor();
        }

        private void OnToggleServer(object? sender, EventArgs e)
        {
            _menu.Hide();

            if (_isRunning)
            {
                StopServer();
                return;
            }

            StartServer();
        }

        private void StartServer()
        {
            if (_isRunning)
            {
                return;
            }

            try
            {
                _app = new App();
                _app.Start();
                _isRunning = true;

                _menu.SetServerRunning(true);
                _trayIcon.Text = "Playback Overlay Server (rodando)";

                _trayIcon.ShowBalloonTip(
                    2000,
                    "Playback Overlay Server",
                    "Servidor iniciado.",
                    ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                _app?.Dispose();
                _app = null;
                _isRunning = false;
                _menu.SetServerRunning(false);

                MessageBox.Show(
                    $"Falha ao iniciar o servidor:\n{ex.Message}",
                    "Playback Overlay Server",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void StopServer()
        {
            if (!_isRunning || _app is null)
            {
                return;
            }

            try
            {
                _app.Dispose();
            }
            finally
            {
                _app = null;
                _isRunning = false;

                _menu.SetServerRunning(false);
                _trayIcon.Text = "Playback Overlay Server (parado)";

                _trayIcon.ShowBalloonTip(
                    2000,
                    "Playback Overlay Server",
                    "Servidor parado.",
                    ToolTipIcon.Info);
            }
        }

        private void OnOpenLogs(object? sender, EventArgs e)
        {
            _menu.Hide();

            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PlaybackDataServer",
                "server.log");

            if (File.Exists(logPath))
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "notepad.exe",
                        Arguments = $"\"{logPath}\"",
                        UseShellExecute = true
                    });

                return;
            }

            MessageBox.Show(
                "Nenhum log encontrado ainda.",
                "Playback Overlay Server",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void OnExit(object? sender, EventArgs e)
        {
            _menu.Hide();
            StopServer();

            _trayIcon.Visible = false;
            _trayIcon.Dispose();

            _menu.Dispose();
            ExitThread();
        }
    }
}
