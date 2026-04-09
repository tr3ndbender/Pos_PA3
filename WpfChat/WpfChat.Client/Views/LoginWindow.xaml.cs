using System;
using System.Threading.Tasks;
using System.Windows;
using WpfChat.Client.Models;
using WpfChat.Client.Network;
using WpfChat.Shared.Models;
using WpfChat.Shared.Protocol;

namespace WpfChat.Client.Views
{
    public partial class LoginWindow : Window
    {
        private ServerConnection? _connection;
        private TaskCompletionSource<AuthResponse>? _authTcs;
        private ClientSettings _settings;

        public LoginWindow(ClientSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            TxtServerIp.Text = settings.LastServerIp;
            TxtPort.Text = settings.LastServerPort.ToString();
            TxtUsername.Text = settings.LastUsername;
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
            => await Authenticate(isRegister: false);

        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
            => await Authenticate(isRegister: true);

        private async Task Authenticate(bool isRegister)
        {
            var ip = TxtServerIp.Text.Trim();
            var portText = TxtPort.Text.Trim();
            var username = TxtUsername.Text.Trim();
            var password = PbPassword.Password;

            if (!int.TryParse(portText, out int port))
            { ShowError("Ungültiger Port."); return; }
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            { ShowError("Bitte Benutzername und Passwort eingeben."); return; }

            SetBusy(true);
            HideError();

            // Alte Verbindung sauber trennen
            if (_connection != null)
            {
                _connection.PacketReceived -= OnPacketReceived;
                _connection.Disconnected -= OnDisconnected;
                _connection.Disconnect();
                _connection = null;
            }

            try
            {
                var conn = new ServerConnection();
                await conn.ConnectAsync(ip, port);

                _authTcs = new TaskCompletionSource<AuthResponse>();

                // Events NACH dem Setzen von _authTcs registrieren
                conn.PacketReceived += OnPacketReceived;
                conn.Disconnected += OnDisconnected;

                _connection = conn;

                var msgType = isRegister ? MessageType.Register : MessageType.Login;
                _connection.Send(ChatPacket.Create(msgType, new AuthRequest
                { Username = username, Password = password }));

                // Warte auf Antwort (max. 8 Sekunden)
                var completed = await Task.WhenAny(_authTcs.Task, Task.Delay(8000));
                AuthResponse? response = completed == _authTcs.Task ? _authTcs.Task.Result : null;

                // Events wieder abmelden damit OnDisconnected nichts mehr macht
                _connection.PacketReceived -= OnPacketReceived;
                _connection.Disconnected -= OnDisconnected;

                if (response == null)
                {
                    _connection.Disconnect();
                    _connection = null;
                    ShowError("Zeitüberschreitung – Server antwortet nicht.");
                    SetBusy(false);
                    return;
                }

                if (!response.Success)
                {
                    _connection.Disconnect();
                    _connection = null;
                    ShowError(response.Message);
                    SetBusy(false);
                    return;
                }

                if (isRegister)
                {
                    // Bei Registrierung: Verbindung trennen, Felder leeren für Login
                    _connection.Disconnect();
                    _connection = null;
                    SetBusy(false);
                    MessageBox.Show("Registrierung erfolgreich! Bitte jetzt anmelden.",
                        "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
                    PbPassword.Clear();
                    return;
                }

                // Login erfolgreich
                _settings.LastServerIp = ip;
                _settings.LastServerPort = port;
                _settings.LastUsername = username;
                _settings.Save();

                // MainWindow ZUERST erstellen und zeigen, dann LoginWindow schließen
                // So gibt es keine Lücke wo die Connection niemanden hat
                var mainWin = new MainWindow(_connection, response.User!);
                mainWin.Show();
                Close();
            }
            catch (Exception ex)
            {
                _connection?.Disconnect();
                _connection = null;
                ShowError($"Verbindungsfehler: {ex.Message}");
                SetBusy(false);
            }
        }

        private void OnPacketReceived(ChatPacket packet)
        {
            if (packet.Type == MessageType.LoginResponse || packet.Type == MessageType.RegisterResponse)
            {
                var resp = packet.GetPayload<AuthResponse>();
                _authTcs?.TrySetResult(resp ?? new AuthResponse
                    { Success = false, Message = "Ungültige Serverantwort" });
            }
        }

        private void OnDisconnected()
        {
            // Nur relevant wenn noch auf Antwort gewartet wird
            _authTcs?.TrySetResult(new AuthResponse
                { Success = false, Message = "Verbindung unerwartet getrennt." });
        }

        private void ShowError(string msg)
        {
            TxtError.Text = msg;
            TxtError.Visibility = Visibility.Visible;
        }

        private void HideError() => TxtError.Visibility = Visibility.Collapsed;

        private void SetBusy(bool busy)
        {
            BtnLogin.IsEnabled = !busy;
            BtnRegister.IsEnabled = !busy;
            TxtServerIp.IsEnabled = !busy;
            TxtPort.IsEnabled = !busy;
            TxtUsername.IsEnabled = !busy;
            PbPassword.IsEnabled = !busy;
        }
    }
}
