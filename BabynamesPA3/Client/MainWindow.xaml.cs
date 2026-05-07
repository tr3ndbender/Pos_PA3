using Network;
using Server;
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

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            client = new TcpClient("127.0.0.1", 12345);
            transfer = new Transfer<MSG>(client);

            transfer.OnMessageReceived += (sender, msg) =>
            {
                if (msg.Type == MSG.MessageType.SEARCHRESULT)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        FoundNameBox.ItemsSource = msg.Names;

                    });
                }

                else if (msg.Type == MSG.MessageType.DETAILRESULT)
                { 
                    Dispatcher.BeginInvoke(() =>
                    {
                        AmountPerYearBox.ItemsSource = msg.Details;
                        GroupByYearBox.ItemsSource = msg.Details;

                    });
                }
                else if (msg.Type == MSG.MessageType.ALTERNATIVERESULT)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        AlternativeNameBox.ItemsSource = msg.Names;

                    });
                }
            };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MSG suchNachricht = new MSG()
            {
                Type = MSG.MessageType.SEARCH,
                Search = SearchNameBox.Text,
                Sex = (SexBox.SelectedItem as ComboBoxItem)?.Tag?.ToString()
            };

            transfer.Send(suchNachricht);
        }

        private void FoundNameBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FoundNameBox.SelectedItem == null) return;

            MSG detailNachricht = new MSG()
            {
                Type = MSG.MessageType.DETAIL,
                Search = FoundNameBox.SelectedItem.ToString(),
                Sex = (SexBox.SelectedItem as ComboBoxItem)?.Tag?.ToString()
            };
            transfer.Send(detailNachricht);

            MSG alternativeNachricht = new MSG()
            {
                Type = MSG.MessageType.ALTERNATIVE,

                Search = FoundNameBox.SelectedItem.ToString(),
                Sex = (SexBox.SelectedItem as ComboBoxItem)?.Tag?.ToString()

            };

            transfer.Send(alternativeNachricht);
        }

        private void AlternativeNameBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AlternativeNameBox.SelectedItem == null) return;

            MSG detailNachricht = new MSG()
            {
                Type = MSG.MessageType.DETAIL,
                Search = AlternativeNameBox.SelectedItem.ToString(),
                Sex = (SexBox.SelectedItem as ComboBoxItem)?.Tag?.ToString()
            };
            transfer.Send(detailNachricht);
        }
    }
}