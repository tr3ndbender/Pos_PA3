using System.IO;
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
using System.Xml.Linq;

namespace Ueben
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        TcpClient _tcp;
        StreamReader _reader;
        StreamWriter _writer;

        string serverWord = "";

        public MainWindow()
        {
            InitializeComponent();

            _tcp = new TcpClient("127.0.0.1", 12345);
            var stream = _tcp.GetStream();
            _reader = new StreamReader(stream);
            _writer = new StreamWriter(stream) { AutoFlush = true };

            // Wort vom Server empfangen
            var xml = _reader.ReadLine();                     // "<Word>HOLIDAY</Word>"
            serverWord = XDocument.Parse(xml).Root.Value;
            fromServerTB.Text = serverWord;
        }

        private void RateButton_Click(object sender, RoutedEventArgs e)
        {
            string antwort = inputTB.Text;
            _writer.WriteLine($"<Antwort>{antwort}</Antwort>");
            Console.WriteLine("test");
        }
    }
}