using System.Collections.ObjectModel;
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
using Wordle.Services;

namespace Wordle
{


    public partial class MainWindow : Window
    {
        private readonly TcpClientService _tcp = new();
        public string word = "";
        private int tries = 0;
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string eingabe = InputTextBox.Text; // ← so holst du den Input
            string antwort = await _tcp.SendMessageAsync(eingabe);
            ResultTextBlock.Text = antwort;
        }

        private void checklength(string word)
        {
            if (word.Length != 7)
            {
                tries++;

            }
        }
    }
}