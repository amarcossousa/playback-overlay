using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PlaybackDataServer.App
{
    /// <summary>
    /// Menu popup próprio, exibido ao clicar no ícone da bandeja.
    /// Ele não usa ContextMenuStrip, portanto layout, hover e margens
    /// não dependem do renderer nativo do Windows.
    /// </summary>
    public sealed class TrayMenuForm : Form
    {
        private static readonly Color MenuBackColor = Color.FromArgb(31, 31, 31);
        private static readonly Color MenuHoverColor = Color.FromArgb(64, 64, 64);
        private static readonly Color MenuForeColor = Color.FromArgb(245, 245, 245);
        private static readonly Color SeparatorColor = Color.FromArgb(74, 74, 74);
        private static readonly Color BorderColor = Color.FromArgb(53, 53, 53);

        private const int MenuWidth = 300;
        private const int MenuPaddingTop = 8;
        private const int MenuPaddingBottom = 8;
        private const int ItemHeight = 42;
        private const int SeparatorHeight = 13;
        private const int TextLeftMargin = 24;
        private const int CornerRadius = 12;

        private readonly MenuActionItem _toggleItem;

        public event EventHandler? ToggleServerClicked;
        public event EventHandler? OpenLogsClicked;
        public event EventHandler? ExitClicked;

        public TrayMenuForm(bool isServerRunning)
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = MenuBackColor;
            ClientSize = new Size(
                MenuWidth,
                MenuPaddingTop + (ItemHeight * 3) + (SeparatorHeight * 2) + MenuPaddingBottom);
            ControlBox = false;
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "Playback Overlay Server";
            TopMost = true;

            _toggleItem = CreateItem(
                isServerRunning ? "Parar servidor" : "Iniciar servidor",
                (_, _) => ToggleServerClicked?.Invoke(this, EventArgs.Empty));

            var logsItem = CreateItem(
                "Abrir logs",
                (_, _) => OpenLogsClicked?.Invoke(this, EventArgs.Empty));

            var exitItem = CreateItem(
                "Sair",
                (_, _) => ExitClicked?.Invoke(this, EventArgs.Empty));

            var y = MenuPaddingTop;

            AddControl(_toggleItem, ref y);
            AddControl(CreateSeparator(), ref y);
            AddControl(logsItem, ref y);
            AddControl(CreateSeparator(), ref y);
            AddControl(exitItem, ref y);
        }

        public void SetServerRunning(bool isRunning)
        {
            _toggleItem.Text = isRunning ? "Parar servidor" : "Iniciar servidor";
        }

        public void ShowNearCursor()
        {
            var cursor = Cursor.Position;
            var workingArea = Screen.FromPoint(cursor).WorkingArea;

            var x = cursor.X - Width + 20;
            var y = cursor.Y - Height - 8;

            if (x < workingArea.Left)
            {
                x = workingArea.Left;
            }

            if (x + Width > workingArea.Right)
            {
                x = workingArea.Right - Width;
            }

            if (y < workingArea.Top)
            {
                y = Math.Min(cursor.Y + 8, workingArea.Bottom - Height);
            }

            Location = new Point(x, y);
            Show();
            Activate();
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Hide();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var borderPen = new Pen(BorderColor);
            using var path = CreateRoundedRectanglePath(
                new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1),
                CornerRadius);

            e.Graphics.DrawPath(borderPen, path);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int WsExToolWindow = 0x00000080;
                const int WsExNoActivate = 0x08000000;

                var createParams = base.CreateParams;
                createParams.ExStyle |= WsExToolWindow | WsExNoActivate;
                return createParams;
            }
        }

        private static MenuActionItem CreateItem(string text, EventHandler onClick)
        {
            var item = new MenuActionItem
            {
                Text = text,
                BackColor = MenuBackColor,
                ForeColor = MenuForeColor,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                Height = ItemHeight,
                Width = MenuWidth,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(TextLeftMargin, 0, 16, 0)
            };

            item.Click += onClick;

            return item;
        }

        private static Panel CreateSeparator()
        {
            return new Panel
            {
                BackColor = MenuBackColor,
                Height = SeparatorHeight,
                Width = MenuWidth,
                Margin = Padding.Empty
            };
        }

        private void AddControl(Control control, ref int y)
        {
            control.Location = new Point(0, y);
            control.Width = ClientSize.Width;
            Controls.Add(control);
            y += control.Height;
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle bounds, int radius)
        {
            var diameter = radius * 2;
            var path = new GraphicsPath();

            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        private sealed class MenuActionItem : Label
        {
            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);
                BackColor = MenuHoverColor;
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                BackColor = MenuBackColor;
            }
        }
    }
}
