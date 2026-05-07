using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using NetworkLibrary;

namespace WPF_Client
{
    public partial class MainWindow : Window
    {
        private TcpClient client = new TcpClient("localhost", 12345);   
        Transfer<Message> t;
        String CurrentUsername;
        public List<String> rooms = new();
        public ObservableCollection<Messages> messages { get; set; } = new();


        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            t = new(client);
            t.OnMessageReceived += (sender, e) =>
            {
                switch (e.Type)
                {
                    case (MessageType.LoginResult):
                        if (e.LoginSucceeded == false)
                        {
                            MessageBox.Show("Anmeldedaten falsch");
                            break;
                        }
                        else
                        {
                            Dispatcher.BeginInvoke(() =>
                            {
                                CurrentUsername = username.Text;
                                loginFenster.Visibility = Visibility.Collapsed;
                                chatFenster.Visibility = Visibility.Visible;
                                rooms = e.UserRooms;
                                MeinTabControl.ItemsSource = e.UserRooms; 
                            });

                            break;
                        }

                    case (MessageType.Registration):
                        if (e.LoginSucceeded == false)
                        { 
                            MessageBox.Show("Es gibt diesen Nutzernamen bereits. Bitte wählen Sie einen anderen.");
                        }
                        else
                        {
                            MessageBox.Show("Sie wurden erfolgreich registriert.");
                        }
                        break;
                    case (MessageType.ServerSendMessages):
                        Dispatcher.Invoke(() =>
                        {
                            foreach (var message in e.Messages)
                            {
                                messages.Add(message);
                            }
                        });

                        break;
                }
            };
        }

        private void loadMessages(String room)
        {
            Dispatcher.Invoke(() =>
            {
                messages.Clear();
            });

            Message m = new Message() { Type = MessageType.ReceiveMessages, Title = room };
            t.SendMessage(m);
        }

        private void Button_Click_Login(object sender, RoutedEventArgs e)
        {
            if (username.Text != "" && password.Text != "")
            {
                Message m = new() { Type = MessageType.Login, Username = username.Text, Password = password.Text };
                t.SendMessage(m);
            }
            else
            {
                MessageBox.Show("Please enter username and password");
            }
        }

        private void Button_Click_Registrate(object sender, RoutedEventArgs e)
        {
            if (username.Text != "" && password.Text != "")
            {
                Message m = new() { Type = MessageType.Registration, Username = username.Text, Password = password.Text };
                t.SendMessage(m);
            }
            else
            {
                MessageBox.Show("Please enter username and password");
            }
        }

        private void ChangedTab(object sender, EventArgs e)
        {
            string? room = MeinTabControl.SelectedItem as string;

            if (room != null)
            {
                loadMessages(room);   
            }
        }

        private void Button_Click_SendMessage(object sender, RoutedEventArgs e)
        {
            if (txtTitle.Text == "" || txtMessage.Text == "")
            {
                MessageBox.Show("Bitte Titel und Text eingeben");
            }

            else
            {
                Dispatcher.Invoke(() =>
                {
                    Message m = new Message() { Type = MessageType.RoomText, Username = CurrentUsername, Title = txtTitle.Text, TheMessage = txtMessage.Text, Room = MeinTabControl.SelectedItem as string };
                    t.SendMessage(m);
                });

                loadMessages(MeinTabControl.SelectedItem as string);
            }
        }
    }
}