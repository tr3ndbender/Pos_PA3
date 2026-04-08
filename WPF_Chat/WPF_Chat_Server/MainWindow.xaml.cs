using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace WPF_Chat_Server
{
    public partial class MainWindow : Window
    {
        private ChatServer _server;
        private Database _db;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chat.db");
            _db = new Database(dbPath);

            int port = int.Parse(PortBox.Text);
            _server = new ChatServer(_db, Log);
            _server.Start(port);

            StatusText.Text = $"Läuft auf Port {port}";
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogList.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
            });
        }
    }
}
