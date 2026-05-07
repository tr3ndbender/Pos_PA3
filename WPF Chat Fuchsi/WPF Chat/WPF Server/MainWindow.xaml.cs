//using DataModels;
using LinqToDB;
using LinqToDB.Data;
using NetworkLibrary;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPF_Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /*public MainWindow()
        {
            InitializeComponent();
            var db = new ChatDB(new DataOptions().UseSQLite(@"Data Source=Model\Chat.db"));
            var table = db.Users.LoadWith(u => u.SentMessages);
            //foreach (var item in table.Where(x => x.Name == "test"))
            //{
            //    protocol.Items.Add(item.ID + " sss" + item.Name + " " + item.Passwort);
            //    protocol.Items.Add(item.SentMessages.First().Title + ": " + item.SentMessages.First().Content);
            //}
            //User user = new() { Name = "ABC", Passwort = "1234" };
            //db.Insert(user);

            Debug.WriteLine("Server aktiv");
            protocol.Items.Add("Server aktiv");
            ThreadPool.QueueUserWorkItem(o => {
                TcpListener server = new TcpListener(IPAddress.Any, 12345);
                server.Start();

                TcpClient client = server.AcceptTcpClient();
                Dispatcher.Invoke(() => { 
                    protocol.Items.Add("Client connected: " + client.Client.RemoteEndPoint);
                });

                Transfer<Message> transfer = new Transfer<Message>(client);

                transfer.OnMessageReceived += (sender, e) =>
                {
                    switch (e.Type)
                    {
                        case (MessageType.Text):
                            Dispatcher.Invoke(() =>
                            {
                                protocol.Items.Add("Client sent message: " + e.TheMessage);
                            });
                            break;

                        case (MessageType.Login):

                            Dispatcher.Invoke(() =>
                            {
                                protocol.Items.Add("Login Attempt from user " + e.Username);
                            });
                            Boolean userExists = false;
                            String firstName = "";

                            foreach (var item in table)
                            {
                                if (item.Name == e.Username & item.Passwort == e.Password)
                                {
                                    userExists = true;
                                    firstName = item.Name;
                                    break;
                                }
                            }
                            if (userExists)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    protocol.Items.Add("Hallo " + firstName);
                                });
                                var UserID = db.Users.Where(u => u.Name == e.Username).First().ID; 

                                var roomsForuser = db.RoomUsers.Where(u => u.User == UserID).LoadWith(u => u.FKRoom);
                                List<String> rooms = new List<String>();

                                foreach (var room in roomsForuser)
                                {
                                    rooms.Add(room.FKRoom.Name);
                                }

                                Message m1 = new() { Type = MessageType.LoginResult, LoginSucceeded = true, UserRooms = rooms };
                                transfer.SendMessage(m1);
                            }
                            else
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    protocol.Items.Add("Anmeldedaten falsch");
                                });
                                Message m2 = new Message() { Type = MessageType.LoginResult, LoginSucceeded = false };
                                transfer.SendMessage(m2);
                            }
                            break;

                        case (MessageType.Registration):
                            Dispatcher.Invoke(() =>
                            {
                                protocol.Items.Add("Registrationsversuch, Username: " + e.Username);
                            });

                            if (!(table.Any(x => x.Name == e.Username)))
                            {
                                Dispatcher.BeginInvoke(() =>
                                {
                                    User user = new User() { Name = e.Username, Passwort = e.Password };
                                    db.Insert(user);
                                    protocol.Items.Add("Added new User: " + e.Username);
                                    Message m = new Message() { Type = MessageType.Registration, LoginSucceeded = true };
                                    transfer.SendMessage(m);
                                });
                                
                            } else
                            {
                                Dispatcher.BeginInvoke(() =>
                                {
                                    protocol.Items.Add("Username " + e.Username + " gibt es schon");
                                    Message m = new Message() { Type = MessageType.Registration, LoginSucceeded = false};
                                    transfer.SendMessage(m);
                                });
                            }

                            break;
                        case (MessageType.ReceiveMessages):
                            var roomID = db.Rooms.Where(u => u.Name == e.Title).First().ID;
                            var messages = db.Messages.Where(u => u.Room == roomID);

                            var sendMessages = new List<Messages>();

                            foreach (var message5 in messages)
                            {
                                var username = db.Users.Where(u => u.ID == message5.Sender).First().Name;
                                sendMessages.Add(new Messages() { Title = message5.Title, TheMessage = message5.Content, Username = username });
                            }

                            Message m = new Message() { Type = MessageType.ServerSendMessages, Messages = sendMessages };
                            transfer.SendMessage(m);

                            break;
                        case (MessageType.RoomText):
                            var senderID = db.Users.Where(u => u.Name == e.Username).First().ID;
                            var roomID1 = db.Rooms.Where(u => u.Name == e.Room).First().ID;

                            DataModels.Message message = new() { Title = e.Title, Content = e.TheMessage, Sender = (long)senderID, Room = roomID1 };
                            db.Insert(message);

                            break;
                    }
                };
            });
        }*/

        

    }
}