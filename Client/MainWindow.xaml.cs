using DataModels;
using Network;
using Server;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.Xml;
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
using System.Xml.Linq;
using static Server.MSG;

namespace Client
{
    public partial class MainWindow : Window
    {
        private int guess = 1;

        TcpClient client;

        Transfer<MSG> transfer;
        private int currentZeile = 0;
        public int FensterBreite = 200;
        
        public ObservableCollection<Cell> Zellen { get; set; } = new();

        public MainWindow()
        {

            InitializeComponent();
            DataContext = this;
            ErstelleGrid();


            client = new TcpClient("127.0.0.1", 12345);

            transfer = new Transfer<MSG>(client);

            transfer.OnMessageReceived += (sender, msg) =>
            {
                if (msg.Type == MessageType.RESPONSE)
                {
                    if (msg.trueOrFalse == true)
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            Fenster.Width = 850;
                            Statistik.Visibility = Visibility.Visible;
                        });

                        switch (guess)
                        {
                            case 1:
                                Dispatcher.BeginInvoke(() => Versuch1Box.Text = "1");
                                break;
                            case 2:
                                Dispatcher.BeginInvoke(() => Versuch2Box.Text = "1");
                                break;
                            case 3:
                                Dispatcher.BeginInvoke(() => Versuch3Box.Text = "1");
                                break;
                            case 4:
                                Dispatcher.BeginInvoke(() => Versuch4Box.Text = "1");
                                break;
                            case 5:
                                Dispatcher.BeginInvoke(() => Versuch5Box.Text = "1");
                                break;
                            case 6:
                                Dispatcher.BeginInvoke(() => Versuch6Box.Text = "1");
                                break;
                        }

                    }

                    for (int i = 0; i < 7; i++)
                    {
                        Brush farbe = Brushes.Gray;
                        if (msg.Results[i] == "G") farbe = Brushes.Green;
                        else if (msg.Results[i] == "Y") farbe = Brushes.Yellow;
                        setCell(currentZeile, i, msg.Wort[i], farbe);
                    }
                    currentZeile++;
                    guess++;
                }
            };
        }

        private void ErstelleGrid()
        {
            for (int i = 0; i < 42; i++)
            {
                Zellen.Add(new Cell());
            }
        }

        public void setCell(int Zeile, int Spalte, char Buchstabe, Brush farbe)
        {
            var Zelle = Zellen[Zeile * 7 + Spalte]; //beginnend bei 0
            Zelle.Buchstabe = Buchstabe;
            Zelle.Farbe = farbe;
        }

        private void submitButton_Click(object sender, RoutedEventArgs e)
        {
            //if ((GuessBox.Text).Length != 7) return;
            MSG guessNachricht = new MSG()
            {
                Type = MessageType.GUESS,
                Wort = GuessBox.Text.ToUpper()
            };

            transfer.Send(guessNachricht);
        }
    }
}