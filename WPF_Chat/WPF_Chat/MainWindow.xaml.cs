using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WPF_Chat
{
    public partial class MainWindow : Window
    {
        // ===== FELDER =====

        // Die TCP-Verbindung zum Server
        private ServerConnection? _connection;

        // Eigene Benutzer-ID (wird nach dem Login gesetzt)
        private int _userId = -1;

        // Eigener Benutzername
        private string _username = "";

        // Einstellungen (Server-IP, Port) – werden als XML gespeichert/geladen
        private ClientSettings _settings;

        // Pfad zur settings.xml – liegt neben der .exe
        private readonly string _settingsPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.xml");

        // ---- Räume ----
        // Dictionary verbindet eine RaumId mit dem zugehörigen TabItem
        private readonly Dictionary<int, TabItem> _roomTabs = new();
        // Dictionary verbindet eine RaumId mit der ListBox, die Nachrichten anzeigt
        private readonly Dictionary<int, ListBox> _roomMessages = new();

        // ---- Private Chats ----
        // Dictionary verbindet die UserId des Gesprächspartners mit dem TabItem
        private readonly Dictionary<int, TabItem> _privateTabs = new();
        // Dictionary verbindet die UserId des Gesprächspartners mit der ListBox
        private readonly Dictionary<int, ListBox> _privateMessages = new();

        // ---- Online-Benutzer ----
        // Dictionary verbindet UserId mit dem OnlineUser-Objekt (Name, Farbe, Bild)
        // Wird gebraucht um bei eingehenden Nachrichten das Profil des Absenders zu finden
        private readonly Dictionary<int, OnlineUser> _onlineUsers = new();

        // ---- Eigenes Profil ----
        private string _myColor = "#000000";       // Eigene Farbe als Hex-String
        private string _myImageBase64 = "";         // Eigenes Bild als Base64-String

        // ===== KONSTRUKTOR =====

        public MainWindow()
        {
            InitializeComponent();

            // Einstellungen laden (falls vorhanden)
            _settings = ClientSettings.Load(_settingsPath);
            ServerIpBox.Text = _settings.ServerIP;
            ServerPortBox.Text = _settings.ServerPort.ToString();

            // Farbvorschau live aktualisieren wenn ColorPicker geändert wird
            // Wird nur EINMAL hier registriert (nicht bei jedem Klick)
            ProfileColorPicker.SelectedColorChanged += (s, args) =>
            {
                if (ProfileColorPicker.SelectedColor.HasValue)
                {
                    var c = ProfileColorPicker.SelectedColor.Value;
                    ColorPreviewText.Foreground = new SolidColorBrush(c);
                    ColorPreviewText.Text = _username != "" ? _username : "Benutzername";
                }
            };
        }

        // Wird beim Schließen des Fensters aufgerufen (Closing-Event aus XAML)
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Aktuell eingegebene Werte in Einstellungen übernehmen
            _settings.ServerIP = ServerIpBox.Text;
            if (int.TryParse(ServerPortBox.Text, out int port))
                _settings.ServerPort = port;

            // Als XML speichern
            _settings.Save(_settingsPath);

            // TCP-Verbindung sauber trennen
            _connection?.Disconnect();
        }

        // ===== VERBINDUNG & LOGIN =====

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Connect()) return;
            // Format: LOGIN|Benutzername|Passwort
            _connection!.Send($"LOGIN|{UsernameBox.Text}|{PasswordBox.Password}");
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Connect()) return;
            // Format: REGISTER|Benutzername|Passwort
            _connection!.Send($"REGISTER|{UsernameBox.Text}|{PasswordBox.Password}");
        }

        // Stellt eine TCP-Verbindung zum Server her.
        // Gibt true zurück wenn die Verbindung erfolgreich war.
        private bool Connect()
        {
            if (_connection != null) return true; // Bereits verbunden

            _connection = new ServerConnection();

            // Event-Handler registrieren:
            // OnMessageReceived wird aufgerufen wenn der Server etwas schickt
            _connection.MessageReceived += OnMessageReceived;
            // OnDisconnected wird aufgerufen wenn die Verbindung getrennt wird
            _connection.Disconnected += OnDisconnected;

            bool ok = _connection.Connect(ServerIpBox.Text, int.Parse(ServerPortBox.Text));
            if (!ok)
            {
                MessageBox.Show("Verbindung zum Server fehlgeschlagen.");
                _connection = null;
                return false;
            }
            return true;
        }

        // ===== NACHRICHTEN VOM SERVER VERARBEITEN =====

        // Wird im Hintergrund-Thread aufgerufen wenn eine Nachricht ankommt.
        private void OnMessageReceived(string message)
        {
            // GUI-Elemente dürfen nur im UI-Thread verändert werden.
            // Dispatcher.Invoke führt den Code im UI-Thread aus.
            Dispatcher.Invoke(() => HandleMessage(message));
        }

        // Verarbeitet eine empfangene Nachricht vom Server.
        private void HandleMessage(string message)
        {
            // Nachricht in max. 5 Teile aufteilen.
            // Das Limit "5" verhindert dass ein Nachrichtentext mit '|' kaputt aufgeteilt wird.
            // Beispiel: "MSG|1|2|Max|Hallo|Welt" → parts = ["MSG","1","2","Max","Hallo|Welt"]
            var parts = message.Split('|', 5);

            switch (parts[0])
            {
                case "LOGIN_OK":
                    // LOGIN_OK|userId|username|color|imageBase64
                    _userId = int.Parse(parts[1]);
                    _username = parts[2];
                    _myColor = parts[3];
                    _myImageBase64 = parts.Length > 4 ? parts[4] : "";

                    // Eigenes Profil in die Online-Liste eintragen
                    AddOrUpdateOnlineUser(_userId, _username, _myColor, _myImageBase64);

                    // Chat-Oberfläche einblenden, Login ausblenden
                    ShowChat();

                    // Räume laden
                    _connection!.Send("GET_ROOMS");

                    // Eigenes Profil an Server schicken → Server schickt es an alle anderen
                    _connection.Send($"UPDATE_PROFILE|{_myColor}|{_myImageBase64}");
                    break;

                case "LOGIN_FAIL":
                    MessageBox.Show("Login fehlgeschlagen. Falscher Benutzername oder Passwort.");
                    break;

                case "REGISTER_OK":
                    MessageBox.Show("Registrierung erfolgreich! Bitte jetzt anmelden.");
                    break;

                case "REGISTER_FAIL":
                    MessageBox.Show("Benutzername bereits vergeben.");
                    break;

                case "ROOM":
                    // ROOM|id|name → Raum zur Raumliste hinzufügen
                    RoomList.Items.Add(new RoomItem { Id = int.Parse(parts[1]), Name = parts[2] });
                    break;

                case "ROOMS_END":
                    // Server hat alle Räume geschickt – hier nichts zu tun
                    break;

                case "ROOM_CREATED":
                    // Ein neuer Raum wurde erstellt → Liste neu laden
                    RoomList.Items.Clear();
                    _connection!.Send("GET_ROOMS");
                    break;

                case "ROOM_EXISTS":
                    MessageBox.Show("Ein Raum mit diesem Namen existiert bereits.");
                    break;

                case "JOINED":
                    // JOINED|roomId → Tab für diesen Raum öffnen
                    AddRoomTab(int.Parse(parts[1]));
                    break;

                case "MSG":
                    // MSG|roomId|senderId|username|text
                    AddMessageToRoom(int.Parse(parts[1]), int.Parse(parts[2]), parts[3], parts[4]);
                    break;

                case "PRIVATE":
                    // PRIVATE|senderId|senderName|text
                    AddPrivateMessage(int.Parse(parts[1]), parts[2], parts[3]);
                    break;

                case "USER_PROFILE":
                    // USER_PROFILE|userId|username|color|imageBase64
                    // Profil eines anderen Benutzers empfangen und speichern
                    AddOrUpdateOnlineUser(
                        int.Parse(parts[1]),
                        parts[2],
                        parts[3],
                        parts.Length > 4 ? parts[4] : "");
                    break;

                case "USER_OFFLINE":
                    // USER_OFFLINE|userId|username → Benutzer aus Online-Liste entfernen
                    RemoveOnlineUser(int.Parse(parts[1]));
                    break;
            }
        }

        // Wird aufgerufen wenn die Verbindung zum Server getrennt wird.
        private void OnDisconnected()
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show("Verbindung zum Server getrennt.");

                // Zurück zum Login-Bildschirm
                LoginPanel.Visibility = Visibility.Visible;
                ChatPanel.Visibility = Visibility.Collapsed;

                // Alle Listen und Tabs leeren
                RoomList.Items.Clear();
                RoomTabs.Items.Clear();
                OnlineList.Items.Clear();
                _roomTabs.Clear();
                _roomMessages.Clear();
                _privateTabs.Clear();
                _privateMessages.Clear();
                _onlineUsers.Clear();

                _connection = null;
                _userId = -1;
            });
        }

        // ===== GUI HILFSMETHODEN =====

        // Blendet das Login-Panel aus und das Chat-Panel ein.
        private void ShowChat()
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            ChatPanel.Visibility = Visibility.Visible;
            Title = $"Chat — {_username}";
        }

        // Erstellt einen neuen Tab für einen Chat-Raum.
        private void AddRoomTab(int roomId)
        {
            if (_roomTabs.ContainsKey(roomId)) return; // Tab gibt es schon

            // Raumname aus der Raumliste holen
            string roomName = $"Raum {roomId}";
            foreach (RoomItem item in RoomList.Items)
            {
                if (item.Id == roomId) { roomName = item.Name; break; }
            }

            // ListBox mit DataTemplate erstellen und als Tab-Inhalt setzen
            var listBox = CreateMessageListBox();
            var tab = new TabItem { Header = roomName, Content = listBox };

            _roomTabs[roomId] = tab;
            _roomMessages[roomId] = listBox;
            RoomTabs.Items.Add(tab);
            RoomTabs.SelectedItem = tab; // Neuen Tab sofort anzeigen
        }

        // Öffnet einen privaten Chat-Tab mit einem anderen Benutzer.
        // Falls der Tab bereits existiert, wird er nur ausgewählt.
        private void OpenPrivateChatTab(int otherUserId, string otherUsername)
        {
            if (_privateTabs.ContainsKey(otherUserId))
            {
                RoomTabs.SelectedItem = _privateTabs[otherUserId]; // Tab auswählen
                return;
            }

            var listBox = CreateMessageListBox();
            var tab = new TabItem
            {
                // "💬" Symbol zeigt visuell dass es ein privater Chat ist
                Header = $"💬 {otherUsername}",
                Content = listBox
            };

            _privateTabs[otherUserId] = tab;
            _privateMessages[otherUserId] = listBox;
            RoomTabs.Items.Add(tab);
            RoomTabs.SelectedItem = tab;
        }

        // Erstellt eine ListBox mit dem MessageTemplate aus den Window-Ressourcen.
        // Dieses Template wird auf alle ChatMessage-Objekte angewendet.
        private ListBox CreateMessageListBox()
        {
            var listBox = new ListBox();
            // FindResource sucht in Window.Resources nach "MessageTemplate"
            listBox.ItemTemplate = (DataTemplate)FindResource("MessageTemplate");
            return listBox;
        }

        // Fügt eine Raum-Nachricht zur entsprechenden ListBox hinzu.
        private void AddMessageToRoom(int roomId, int senderId, string senderName, string text)
        {
            if (!_roomMessages.ContainsKey(roomId)) return;
            var msg = BuildChatMessage(senderId, senderName, text);
            AddToListBox(_roomMessages[roomId], msg);
        }

        // Fügt eine private Nachricht zur entsprechenden ListBox hinzu.
        // Öffnet automatisch einen Tab falls noch keiner existiert.
        private void AddPrivateMessage(int senderId, string senderName, string text)
        {
            OpenPrivateChatTab(senderId, senderName);
            var msg = BuildChatMessage(senderId, senderName, text);
            AddToListBox(_privateMessages[senderId], msg);
        }

        // Erstellt ein ChatMessage-Objekt mit Profil-Daten des Absenders.
        // Schlägt Farbe und Bild aus dem _onlineUsers-Dictionary nach.
        private ChatMessage BuildChatMessage(int senderId, string senderName, string text)
        {
            SolidColorBrush color = Brushes.Black; // Standardfarbe falls kein Profil bekannt
            BitmapImage? image = null;

            // Profil des Absenders nachschlagen
            if (_onlineUsers.TryGetValue(senderId, out var profile))
            {
                color = profile.Brush;
                image = profile.Image;
            }

            return new ChatMessage
            {
                Username = senderName,
                Text = text,
                Color = color,
                Image = image
            };
        }

        // Fügt ein Element zur ListBox hinzu und scrollt automatisch nach unten.
        private static void AddToListBox(ListBox listBox, object item)
        {
            listBox.Items.Add(item);
            listBox.ScrollIntoView(item);
        }

        // ===== ONLINE-BENUTZER =====

        // Fügt einen Benutzer zur Online-Liste hinzu oder aktualisiert sein Profil.
        // Wird aufgerufen wenn: eigener Login, USER_PROFILE vom Server empfangen
        private void AddOrUpdateOnlineUser(int userId, string username, string color, string imageBase64)
        {
            // Base64-String in BitmapImage umwandeln
            BitmapImage? image = Base64ToImage(imageBase64);

            // Farbstring (z.B. "#FF0000") in WPF-SolidColorBrush umwandeln
            SolidColorBrush brush;
            try { brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)); }
            catch { brush = Brushes.Black; } // Ungültige Farbe → Schwarz

            if (_onlineUsers.ContainsKey(userId))
            {
                // Benutzer aktualisieren
                _onlineUsers[userId].Username = username;
                _onlineUsers[userId].Color = color;
                _onlineUsers[userId].Image = image;
                _onlineUsers[userId].Brush = brush;
            }
            else
            {
                // Neuen Benutzer anlegen
                _onlineUsers[userId] = new OnlineUser
                {
                    Id = userId,
                    Username = username,
                    Color = color,
                    Image = image,
                    Brush = brush
                };
            }

            RefreshOnlineList();
        }

        private void RemoveOnlineUser(int userId)
        {
            _onlineUsers.Remove(userId);
            RefreshOnlineList();
        }

        // Baut die Online-Benutzerliste in der GUI neu auf.
        private void RefreshOnlineList()
        {
            OnlineList.Items.Clear();
            foreach (var user in _onlineUsers.Values)
            {
                // Sich selbst nicht in der Liste anzeigen
                if (user.Id != _userId)
                    OnlineList.Items.Add(user);
            }
        }

        // ===== BUTTON-EVENTS =====

        private void CreateRoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewRoomBox.Text)) return;
            _connection!.Send($"CREATE_ROOM|{NewRoomBox.Text}");
            NewRoomBox.Clear();
        }

        private void JoinRoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (RoomList.SelectedItem is RoomItem room)
                _connection!.Send($"JOIN_ROOM|{room.Id}");
        }

        private void RoomList_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        // Senden-Button
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendCurrentMessage();
        }

        // Enter-Taste im Textfeld sendet die Nachricht
        private void MessageInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SendCurrentMessage();
        }

        // Sendet die eingegebene Nachricht.
        // Erkennt ob der aktive Tab ein Raum oder ein privater Chat ist.
        private void SendCurrentMessage()
        {
            if (string.IsNullOrWhiteSpace(MessageInput.Text)) return;
            if (RoomTabs.SelectedItem is not TabItem selectedTab) return;

            string text = MessageInput.Text;

            // In Raum-Tabs suchen
            foreach (var kvp in _roomTabs)
            {
                if (kvp.Value == selectedTab)
                {
                    _connection!.Send($"MSG|{kvp.Key}|{text}");
                    // Eigene Nachricht sofort anzeigen (ohne auf Server zu warten)
                    AddMessageToRoom(kvp.Key, _userId, _username, text);
                    MessageInput.Clear();
                    return;
                }
            }

            // In privaten Chat-Tabs suchen
            foreach (var kvp in _privateTabs)
            {
                if (kvp.Value == selectedTab)
                {
                    _connection!.Send($"PRIVATE|{kvp.Key}|{text}");
                    // Eigene Nachricht sofort anzeigen
                    AddToListBox(_privateMessages[kvp.Key],
                        BuildChatMessage(_userId, _username, text));
                    MessageInput.Clear();
                    return;
                }
            }
        }

        // Öffnet einen privaten Chat mit dem ausgewählten Online-Benutzer.
        private void PrivateMessageButton_Click(object sender, RoutedEventArgs e)
        {
            if (OnlineList.SelectedItem is OnlineUser user)
                OpenPrivateChatTab(user.Id, user.Username);
            else
                MessageBox.Show("Bitte zuerst einen Benutzer aus der Online-Liste auswählen.");
        }

        // ===== PROFIL =====

        // Öffnet einen FileDialog zum Auswählen eines Profilbilds.
        private void SelectImageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Profilbild auswählen",
                Filter = "Bilder|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (dialog.ShowDialog() != true) return;

            // Bild laden und auf 50px Breite skalieren.
            // DecodePixelWidth skaliert beim Laden – das ist effizienter als danach skalieren.
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.DecodePixelWidth = 50; // Automatisch auf 50px Breite (Höhe proportional)
            bi.UriSource = new Uri(dialog.FileName);
            bi.EndInit();

            // BitmapImage → Base64-String umwandeln für Netzwerkübertragung
            _myImageBase64 = ImageToBase64(bi);

            // Sofort an Server schicken → Server verteilt es an alle anderen Clients
            _connection?.Send($"UPDATE_PROFILE|{_myColor}|{_myImageBase64}");

            // Eigenes Profil aktualisieren damit eigene Nachrichten auch das Bild zeigen
            AddOrUpdateOnlineUser(_userId, _username, _myColor, _myImageBase64);

            MessageBox.Show("Profilbild wurde gespeichert und übertragen!");
        }

        // Öffnet das Farb-Auswahl-Panel.
        private void SelectColorMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Aktuelle Farbe im ColorPicker vorauswählen
            try
            {
                ProfileColorPicker.SelectedColor =
                    (Color)ColorConverter.ConvertFromString(_myColor);
            }
            catch { }

            // Benutzername in der Vorschau anzeigen
            ColorPreviewText.Text = _username != "" ? _username : "Benutzername";

            // Panel sichtbar machen (liegt über dem Chat)
            ProfilePanel.Visibility = Visibility.Visible;
        }

        // Speichert die gewählte Farbe und schickt sie an den Server.
        private void SaveColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileColorPicker.SelectedColor.HasValue)
            {
                var c = ProfileColorPicker.SelectedColor.Value;
                // Farbe als Hex-String speichern (z.B. "#FF0000" für Rot)
                _myColor = $"#{c.R:X2}{c.G:X2}{c.B:X2}";

                // An Server schicken → wird in DB gespeichert und an alle verteilt
                _connection?.Send($"UPDATE_PROFILE|{_myColor}|{_myImageBase64}");

                // Eigenes Profil in der Online-Liste aktualisieren
                AddOrUpdateOnlineUser(_userId, _username, _myColor, _myImageBase64);
            }

            ProfilePanel.Visibility = Visibility.Collapsed;
        }

        // Schließt das Farb-Panel ohne Änderungen.
        private void CancelProfileButton_Click(object sender, RoutedEventArgs e)
        {
            ProfilePanel.Visibility = Visibility.Collapsed;
        }

        // ===== HILFSMETHODEN FÜR BILDER =====

        // Konvertiert einen Base64-String zurück in ein BitmapImage.
        // Wird verwendet um empfangene Profilbilder (als Text übertragen) anzuzeigen.
        private static BitmapImage? Base64ToImage(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return null;

            try
            {
                // Base64 → Byte-Array → MemoryStream → BitmapImage
                byte[] bytes = Convert.FromBase64String(base64);
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.DecodePixelWidth = 50;
                bi.StreamSource = new MemoryStream(bytes);
                bi.EndInit();
                bi.Freeze(); // Einfrieren damit es thread-sicher ist (für Dispatcher.Invoke)
                return bi;
            }
            catch
            {
                return null; // Fehlerhafte Bilder einfach ignorieren
            }
        }

        // Konvertiert ein BitmapImage in einen Base64-String.
        // Wird verwendet um das Bild als Text über das Netzwerk zu schicken.
        private static string ImageToBase64(BitmapImage image)
        {
            // PngBitmapEncoder kodiert das Bild als PNG
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using var ms = new MemoryStream();
            encoder.Save(ms); // PNG-Daten in MemoryStream schreiben
            return Convert.ToBase64String(ms.ToArray()); // Byte-Array → Base64-String
        }
    }

    // ===== HILFSKLASSEN =====

    // Repräsentiert einen Chat-Raum in der Raumliste (links).
    public class RoomItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        // ToString() wird von der ListBox verwendet um den Raum als Text anzuzeigen
        public override string ToString() => Name;
    }

    // Repräsentiert einen online eingeloggten Benutzer.
    public class OnlineUser
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Color { get; set; } = "#000000"; // Farbe als Hex-String
        public SolidColorBrush Brush { get; set; } = Brushes.Black; // Farbe als WPF-Brush
        public BitmapImage? Image { get; set; }  // Profilbild
        public override string ToString() => Username;
    }
}
