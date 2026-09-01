using System.Windows.Forms;

namespace PlaybackDataServer.App
{
    // Ícone na bandeja com controle manual de ligar/desligar o servidor.
    // O processo (e o ícone) ficam sempre rodando; o servidor WebSocket/NPSM
    // só ativa quando você escolhe "Iniciar" no menu.
    public sealed class TrayApp : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly ToolStripMenuItem _toggleItem;
        private App? _app;
        private bool _isRunning;

        public TrayApp()
        {
            _toggleItem = new ToolStripMenuItem("Iniciar servidor", null, OnToggleServer);

            var menu = new ContextMenuStrip();
            menu.Items.Add(_toggleItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Abrir logs", null, OnOpenLogs);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Sair", null, OnExit);

            _trayIcon = new NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application, // troque pelo seu .ico depois
                Text = "Playback Overlay Server (parado)",
                ContextMenuStrip = menu,
                Visible = true
            };

            _trayIcon.DoubleClick += OnToggleServer;

            StartServer();
        }

        private void OnToggleServer(object? sender, EventArgs e)
        {
            if (_isRunning)
            {
                StopServer();
            }
            else
            {
                StartServer();
            }
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

                _toggleItem.Text = "Parar servidor";
                _trayIcon.Text = "Playback Overlay Server (rodando)";
                _trayIcon.ShowBalloonTip(2000, "Playback Overlay Server", "Servidor iniciado.", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
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

            _app.Dispose();
            _app = null;
            _isRunning = false;

            _toggleItem.Text = "Iniciar servidor";
            _trayIcon.Text = "Playback Overlay Server (parado)";
            _trayIcon.ShowBalloonTip(2000, "Playback Overlay Server", "Servidor parado.", ToolTipIcon.Info);
        }

        private void OnOpenLogs(object? sender, EventArgs e)
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PlaybackDataServer", "server.log");

            if (File.Exists(logPath))
            {
                System.Diagnostics.Process.Start("notepad.exe", logPath);
            }
            else
            {
                MessageBox.Show("Nenhum log encontrado ainda.", "Playback Overlay Server");
            }
        }

        private void OnExit(object? sender, EventArgs e)
        {
            StopServer();
            _trayIcon.Visible = false;
            Application.Exit();
        }
    }
}