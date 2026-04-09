using System;
using System.Windows;
using WpfChat.Server.Database;
using WpfChat.Server.Network;

namespace WpfChat.Server
{
    public partial class MainWindow : Window
    {
        private ChatServer? _server;
        private DatabaseManager? _db;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtPort.Text, out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Ungültiger Port.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Pfad: WpfChat.Server\Database\chat.db (Projektordner, nicht bin)
                var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                var projectDir = System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(exeDir, "..", "..", ".."));
                var dbPath = System.IO.Path.Combine(projectDir, "Database", "chat.db");

                _db = new DatabaseManager(dbPath);
                _server = new ChatServer(_db);
                _server.OnLog += AppendLog;
                _server.Starten(port);
                AppendLog($"[DB] Pfad: {dbPath}");

                LblStatus.Content = $"Läuft auf Port {port}";
                LblStatus.Foreground = System.Windows.Media.Brushes.Green;
                BtnStart.IsEnabled = false;
                BtnStop.IsEnabled = true;
                TxtPort.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Starten: {ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _server?.Stoppen();
            LblStatus.Content = "Gestoppt";
            LblStatus.Foreground = System.Windows.Media.Brushes.Red;
            BtnStart.IsEnabled = true;
            BtnStop.IsEnabled = false;
            TxtPort.IsEnabled = true;
        }

        private void AppendLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LstLog.Items.Add(message);
                LstLog.ScrollIntoView(LstLog.Items[^1]);
            });
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            LstLog.Items.Clear();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _server?.Stoppen();
        }
    }
}
