using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfChat.Client.Helpers;
using WpfChat.Client.Models;
using WpfChat.Client.Network;
using WpfChat.Shared.Models;
using WpfChat.Shared.Protocol;

namespace WpfChat.Client.Views
{
    public partial class MainWindow : Window
    {
        private readonly ServerConnection _connection;
        private UserDto _me;

        private readonly ObservableCollection<RoomDto> _rooms = new();
        private readonly ObservableCollection<OnlineUserViewModel> _onlineUsers = new();
        private readonly ObservableCollection<ChatTabItem> _tabs = new();

        // profile image cache: username -> base64
        private readonly Dictionary<string, string> _profileCache = new();

        public MainWindow(ServerConnection connection, UserDto me)
        {
            InitializeComponent();

            _connection = connection;
            _me = me;

            // Wire up
            LstRooms.ItemsSource = _rooms;
            LstUsers.ItemsSource = _onlineUsers;
            TabChats.ItemsSource = _tabs;

            _connection.PacketReceived += OnPacketReceived;
            _connection.Disconnected += OnServerDisconnected;

            UpdateMyProfile();

            // Räume laden sobald das Fenster vollständig geladen ist
            Loaded += (_, _) =>
                _connection.Send(ChatPacket.Create(MessageType.GetRooms, new { }));
        }

        // ── Profile ───────────────────────────────────────────
        private void UpdateMyProfile()
        {
            TxtMyUsername.Text = _me.Username;
            TxtStatusUser.Text = _me.Username;

            if (!string.IsNullOrEmpty(_me.ProfileImageBase64))
            {
                ImgMyAvatar.Source = ImageHelper.FromBase64(_me.ProfileImageBase64, 80);
                ImgStatusAvatar.Source = ImageHelper.FromBase64(_me.ProfileImageBase64, 20);
            }

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(_me.Color);
                var brush = new SolidColorBrush(color);
                RctMyColor.Fill = brush;
                RctStatusColor.Fill = brush;
            }
            catch { }
        }

        private void MnuSelectAvatar_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Bilder|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                Title = "Profilbild auswählen"
            };
            if (dlg.ShowDialog() != true) return;

            var img = ImageHelper.LoadAndResize(dlg.FileName, 50);
            if (img == null) { System.Windows.MessageBox.Show("Bild konnte nicht geladen werden."); return; }

            ImgMyAvatar.Source = img;
            ImgStatusAvatar.Source = img;

            var base64 = ImageHelper.ToBase64(img);
            _me.ProfileImageBase64 = base64;
            _profileCache[_me.Username] = base64 ?? "";

            SendProfileUpdate();
        }

        private void MnuChangeColor_Click(object sender, RoutedEventArgs e)
        {
            var picker = new ColorPickerWindow();
            try
            {
                var current = (Color)ColorConverter.ConvertFromString(_me.Color);
                picker.SelectedColorValue = current;
            }
            catch { }

            if (picker.ShowDialog() == true)
            {
                var c = picker.SelectedColor;
                _me.Color = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
                var brush = new SolidColorBrush(c);
                RctMyColor.Fill = brush;
                RctStatusColor.Fill = brush;
                SendProfileUpdate();
            }
        }

        private void SendProfileUpdate()
        {
            _connection.Send(ChatPacket.Create(MessageType.UpdateProfile, new UpdateProfileRequest
            {
                Color = _me.Color,
                ProfileImageBase64 = _me.ProfileImageBase64
            }));
        }

        // ── Rooms ─────────────────────────────────────────────
        private void MnuCreateRoom_Click(object sender, RoutedEventArgs e)
        {
            var name = ShowInputDialog("Raumname:", "Neuen Raum erstellen");
            if (string.IsNullOrWhiteSpace(name)) return;
            _connection.Send(ChatPacket.Create(MessageType.CreateRoom, new RoomRequest { RoomName = name }));
        }

        private void MnuJoinRoom_Click(object sender, RoutedEventArgs e) => JoinSelectedRoom();
        private void BtnJoinRoom_Click(object sender, RoutedEventArgs e) => JoinSelectedRoom();

        private void LstRooms_DoubleClick(object sender, MouseButtonEventArgs e) => JoinSelectedRoom();

        private void LstRooms_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) JoinSelectedRoom();
        }

        private void JoinSelectedRoom()
        {
            if (LstRooms.SelectedItem is RoomDto room)
                JoinRoom(room.Name);
        }

        private void JoinRoom(string roomName)
        {
            if (_tabs.Any(t => t.Name == roomName && !t.IsPrivate)) return;
            _connection.Send(ChatPacket.Create(MessageType.JoinRoom, new RoomRequest { RoomName = roomName }));
        }

        // ── Private Messages ──────────────────────────────────
        private void MnuPrivateMsg_Click(object sender, RoutedEventArgs e)
        {
            var users = _onlineUsers.Select(u => u.Username).Where(u => u != _me.Username).ToList();
            if (!users.Any()) { System.Windows.MessageBox.Show("Keine anderen Benutzer online."); return; }

            var username = ShowSelectionDialog("Empfänger auswählen:", users);
            if (string.IsNullOrEmpty(username)) return;
            OpenPrivateTab(username);
        }

        private void LstUsers_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (LstUsers.SelectedItem is OnlineUserViewModel user && user.Username != _me.Username)
                OpenPrivateTab(user.Username);
        }

        private void OpenPrivateTab(string username)
        {
            var tabName = $"PM: {username}";
            if (_tabs.Any(t => t.Name == tabName)) { SelectTab(tabName); return; }

            var tab = new ChatTabItem { Name = tabName, IsPrivate = true };
            _tabs.Add(tab);
            TabChats.SelectedItem = tab;
        }

        // ── Send Messages ─────────────────────────────────────
        private void BtnSend_Click(object sender, RoutedEventArgs e) => SendCurrentMessage();
        private void TxtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift))
            {
                SendCurrentMessage();
                e.Handled = true;
            }
        }

        private void SendCurrentMessage()
        {
            var text = TxtMessage.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;
            if (TabChats.SelectedItem is not ChatTabItem tab) return;

            if (tab.IsPrivate)
            {
                var recipient = tab.Name.Replace("PM: ", "");
                _connection.Send(ChatPacket.Create(MessageType.PrivateMessage, new ChatMessageDto
                {
                    Content = text,
                    RecipientUsername = recipient,
                    SenderUsername = _me.Username
                }));
            }
            else
            {
                _connection.Send(ChatPacket.Create(MessageType.ChatMessage, new ChatMessageDto
                {
                    Content = text,
                    RoomName = tab.Name,
                    SenderUsername = _me.Username
                }));
            }

            TxtMessage.Clear();
        }

        // ── Packet Handling ───────────────────────────────────
        private void OnPacketReceived(ChatPacket packet)
        {
            Dispatcher.Invoke(() => HandlePacket(packet));
        }

        private void HandlePacket(ChatPacket packet)
        {
            switch (packet.Type)
            {
                case MessageType.RoomsResponse:
                {
                    var rooms = packet.GetPayload<List<RoomDto>>();
                    if (rooms != null) { _rooms.Clear(); foreach (var r in rooms) _rooms.Add(r); }
                    break;
                }
                case MessageType.RoomCreated:
                {
                    var newRoom = packet.GetPayload<RoomDto>();
                    if (newRoom != null && _rooms.All(r => r.Name != newRoom.Name))
                        _rooms.Add(newRoom);
                    break;
                }
                case MessageType.RoomJoined:
                {
                    var jr = packet.GetPayload<JoinRoomResponse>();
                    if (jr?.Room != null)
                    {
                        var tab = new ChatTabItem { Name = jr.Room.Name, IsPrivate = false };
                        foreach (var msg in jr.RecentMessages)
                        {
                            EnsureProfileCached(msg.SenderUsername, msg.SenderProfileImageBase64);
                            tab.Messages.Add(new MessageViewModel(msg));
                        }
                        _tabs.Add(tab);
                        TabChats.SelectedItem = tab;
                    }
                    break;
                }
                case MessageType.RoomLeft:
                {
                    var leftRoom = packet.GetPayload<RoomRequest>();
                    if (leftRoom != null)
                    {
                        var t = _tabs.FirstOrDefault(x => x.Name == leftRoom.RoomName);
                        if (t != null) _tabs.Remove(t);
                    }
                    break;
                }
                case MessageType.ChatMessage:
                {
                    var chatMsg = packet.GetPayload<ChatMessageDto>();
                    if (chatMsg != null)
                    {
                        EnsureProfileCached(chatMsg.SenderUsername, chatMsg.SenderProfileImageBase64);
                        var msgVm = new MessageViewModel(chatMsg);
                        var roomTab = _tabs.FirstOrDefault(t => t.Name == chatMsg.RoomName && !t.IsPrivate);
                        if (roomTab != null)
                        {
                            roomTab.Messages.Add(msgVm);
                            ScrollToBottom(roomTab);
                            if (TabChats.SelectedItem != roomTab)
                                roomTab.HasUnread = true;
                        }
                    }
                    break;
                }
                case MessageType.PrivateMessage:
                {
                    var pm = packet.GetPayload<ChatMessageDto>();
                    if (pm != null)
                    {
                        EnsureProfileCached(pm.SenderUsername, pm.SenderProfileImageBase64);
                        var otherUser = pm.SenderUsername == _me.Username ? pm.RecipientUsername! : pm.SenderUsername;
                        var pmTabName = $"PM: {otherUser}";
                        var pmTab = _tabs.FirstOrDefault(t => t.Name == pmTabName);
                        if (pmTab == null)
                        {
                            pmTab = new ChatTabItem { Name = pmTabName, IsPrivate = true };
                            _tabs.Add(pmTab);
                        }
                        pmTab.Messages.Add(new MessageViewModel(pm));
                        ScrollToBottom(pmTab);
                        if (TabChats.SelectedItem != pmTab)
                            pmTab.HasUnread = true;
                    }
                    break;
                }
                case MessageType.UserJoined:
                {
                    var joinedUser = packet.GetPayload<UserDto>();
                    if (joinedUser != null && _onlineUsers.All(u => u.Username != joinedUser.Username))
                        _onlineUsers.Add(new OnlineUserViewModel(joinedUser));
                    break;
                }
                case MessageType.UserLeft:
                {
                    var leftUser = packet.GetPayload<UserDto>();
                    if (leftUser != null)
                    {
                        var existing = _onlineUsers.FirstOrDefault(u => u.Username == leftUser.Username);
                        if (existing != null) _onlineUsers.Remove(existing);
                    }
                    break;
                }
                case MessageType.UpdateProfile:
                {
                    var updatedUser = packet.GetPayload<UserDto>();
                    if (updatedUser != null)
                    {
                        if (!string.IsNullOrEmpty(updatedUser.ProfileImageBase64))
                            _profileCache[updatedUser.Username] = updatedUser.ProfileImageBase64;
                        var vm = _onlineUsers.FirstOrDefault(u => u.Username == updatedUser.Username);
                        vm?.UpdateFrom(updatedUser);
                        if (!string.IsNullOrEmpty(updatedUser.ProfileImageBase64))
                            UpdateMessageAvatars(updatedUser.Username, updatedUser.ProfileImageBase64);
                    }
                    break;
                }
                case MessageType.Error:
                {
                    var errMsg = packet.GetPayload<string>();
                    if (!string.IsNullOrEmpty(errMsg))
                        System.Windows.MessageBox.Show(errMsg, "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                }
            }
        }

        private void EnsureProfileCached(string username, string? base64)
        {
            if (!string.IsNullOrEmpty(base64) && !_profileCache.ContainsKey(username))
                _profileCache[username] = base64;
        }

        private void UpdateMessageAvatars(string username, string base64)
        {
            foreach (var tab in _tabs)
                foreach (var msg in tab.Messages.Where(m => m.SenderUsername == username))
                    msg.SetProfileImage(base64);
        }

        private void ScrollToBottom(ChatTabItem tab)
        {
            // Warte bis WPF das neue Element gerendert hat, dann scroll runter
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
            {
                try
                {
                    // VisualTree nach ListView durchsuchen
                    var lv = FindChild<ListView>(TabChats);
                    if (lv != null && lv.Items.Count > 0)
                        lv.ScrollIntoView(lv.Items[^1]);
                }
                catch { }
            });
        }

        // Sucht rekursiv ein Control eines bestimmten Typs im VisualTree
        private static T? FindChild<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
        {
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var result = FindChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        private void SelectTab(string name)
        {
            var tab = _tabs.FirstOrDefault(t => t.Name == name);
            if (tab != null) TabChats.SelectedItem = tab;
        }

        private void TabChats_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabChats.SelectedItem is ChatTabItem tab)
                tab.HasUnread = false;
        }

        private void BtnCloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ChatTabItem tab)
            {
                if (!tab.IsPrivate)
                    _connection.Send(ChatPacket.Create(MessageType.LeaveRoom,
                        new RoomRequest { RoomName = tab.Name }));
                _tabs.Remove(tab);
            }
        }

        // ── Disconnect / Exit ──────────────────────────────────
        private void MnuDisconnect_Click(object sender, RoutedEventArgs e)
        {
            _connection.Disconnect();
            App.ShowLogin();
            Close();
        }

        private void MnuExit_Click(object sender, RoutedEventArgs e)
        {
            _connection.Disconnect();
            System.Windows.Application.Current.Shutdown();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _connection.Disconnect();
        }

        private void OnServerDisconnected()
        {
            Dispatcher.Invoke(() =>
            {
                System.Windows.MessageBox.Show("Verbindung zum Server getrennt.", "Getrennt",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                App.ShowLogin();
                Close();
            });
        }

        // ── Helpers ───────────────────────────────────────────
        private static string? ShowInputDialog(string prompt, string title)
        {
            var win = new Window
            {
                Title = title, Width = 360, Height = 140,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize
            };
            var sp = new StackPanel { Margin = new Thickness(16) };
            sp.Children.Add(new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 6) });
            var tb = new TextBox { Height = 30, Padding = new Thickness(4) };
            sp.Children.Add(tb);
            var btnOk = new Button { Content = "OK", Width = 80, Height = 30, Margin = new Thickness(0, 10, 0, 0) };
            btnOk.Click += (_, _) => win.DialogResult = true;
            sp.Children.Add(btnOk);
            win.Content = sp;
            tb.Focus();
            return win.ShowDialog() == true ? tb.Text : null;
        }

        private static string? ShowSelectionDialog(string prompt, List<string> options)
        {
            var win = new Window
            {
                Title = prompt, Width = 300, Height = 260,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize
            };
            var sp = new StackPanel { Margin = new Thickness(16) };
            sp.Children.Add(new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 6) });
            var lb = new ListBox { Height = 140 };
            foreach (var o in options) lb.Items.Add(o);
            sp.Children.Add(lb);
            var btnOk = new Button { Content = "OK", Width = 80, Height = 30, Margin = new Thickness(0, 10, 0, 0) };
            btnOk.Click += (_, _) => { if (lb.SelectedItem != null) win.DialogResult = true; };
            sp.Children.Add(btnOk);
            win.Content = sp;
            return win.ShowDialog() == true ? lb.SelectedItem as string : null;
        }
    }

    /// <summary>Simple color picker window wrapping the Extended WPF Toolkit ColorPicker.</summary>
    public class ColorPickerWindow : Window
    {
        private readonly Xceed.Wpf.Toolkit.ColorPicker _picker;
        public Color SelectedColor => _picker.SelectedColor ?? Colors.Black;

        public ColorPickerWindow()
        {
            Title = "Farbe auswählen";
            Width = 300; Height = 160;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            _picker = new Xceed.Wpf.Toolkit.ColorPicker
            {
                Margin = new Thickness(12),
                ShowAvailableColors = true,
                ShowStandardColors = true
            };

            var sp = new StackPanel { Margin = new Thickness(8) };
            sp.Children.Add(_picker);
            var btnOk = new Button { Content = "OK", Width = 80, Height = 30, Margin = new Thickness(0, 8, 0, 0) };
            btnOk.Click += (_, _) => DialogResult = true;
            sp.Children.Add(btnOk);
            Content = sp;
        }

        public Color? SelectedColorValue
        {
            get => _picker.SelectedColor;
            set => _picker.SelectedColor = value;
        }
    }
}
