namespace PlaybackDataServer
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PlaybackDataServer");
            Directory.CreateDirectory(logDir);

            var logPath = Path.Combine(logDir, "server.log");
            var logWriter = new StreamWriter(logPath, append: true) { AutoFlush = true };
            Console.SetOut(logWriter);
            Console.SetError(logWriter);

            ApplicationConfiguration.Initialize();
            System.Windows.Forms.Application.Run(new App.TrayApp());
        }
    }
}