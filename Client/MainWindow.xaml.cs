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

        TcpClient client;

        Transfer<MSG> transfer;

        public ObservableCollection<string> FoundNames { get; set; } = new();
        public ObservableCollection<string> AlternativeNames { get; set; } = new();
        public List<long> Jahre = new List<long>();
        public List<long> Count = new List<long>();
        public MainWindow()
        {
            
            InitializeComponent();
            DataContext = this;


            client = new TcpClient("127.0.0.1", 12345);

            transfer = new Transfer<MSG>(client);

            transfer.OnMessageReceived += (sender, msg) => {
                // Achtung: kommt aus einem anderen Thread!
                // msg.Names enthält z.B. die gefundenen Namen

                if (msg.Type == MessageType.SEARCHRESULT)
                {
                    Dispatcher.BeginInvoke(() => { //BeginInvoke, weil sonst NetzwerkThread blockiert wird
                        FoundNames.Clear();
                        // Hier bist du wieder im UI-Thread — ObservableCollection befüllen ist sicher
                        foreach (string name in msg.Names)
                        {
                            FoundNames.Add(name);
                        }
                    });
                }
                else if (msg.Type == MessageType.ALTERNATIVERESULT)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        AlternativeNamesComboBox.ItemsSource = msg.Names;
                    });
                }
                else if (msg.Type == MessageType.DETAILRESULT)
                {
                    Jahre.Clear();
                    Count.Clear();

                    foreach (Babyname b in msg.Details)
                    {
                        Jahre.Add(b.Year);
                        Count.Add(b.Count);
                    }

                    Dispatcher.BeginInvoke(() =>
                    {
                        GroupByYearListBox.ItemsSource = Jahre;
                        anzListBox.ItemsSource = Count;
                    });
                }

            };

        }

        private void suchButton_Click(object sender, RoutedEventArgs e)
        {
            /* Methode 1
            string gender = "";

            if ((GenderComboBox.SelectedItem as ComboBoxItem).ToString() == "maennlich")
            {
                gender = "M";
            }
            else
            {
                gender = "W";
            }
            */


            MSG suchNachricht = new MSG()
            {
                Type = MessageType.SEARCH,
                Search = namensSucheTB.Text,
                Sex = (GenderComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() // bessere Methode 

            };

            transfer.Send(suchNachricht);

        }

        private void FoundNamesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FoundNamesComboBox.SelectedItem == null) return;

            MSG alternativeNachricht = new MSG()
            {
                Type = MessageType.ALTERNATIVE,
                Sex = (GenderComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString(),
                //Search = (FoundNamesComboBox.SelectedItem as ComboBoxItem).ToString(),
                // -> ist falsch weil man nur braucht, wenn man manuell im xaml die items definiert
                Search = FoundNamesComboBox.SelectedItem.ToString()
            };

            transfer.Send(alternativeNachricht);
        }

        private void AlternativeNamesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FoundNamesComboBox.SelectedItem == null) return;

            MSG detailNachricht = new MSG()
            {
                Type = MessageType.DETAIL,
                Sex = (GenderComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString(),
                Search = FoundNamesComboBox.SelectedItem.ToString()
            };

            transfer.Send(detailNachricht);
        }
    }
}