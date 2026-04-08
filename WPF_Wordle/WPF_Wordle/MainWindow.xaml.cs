using System.Collections.ObjectModel;
using System.IO;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;
using WPF_Wordle.Models;

namespace WPF_Wordle
{
    public partial class MainWindow : Window
    {

        // Verbindung
        TcpClient _tcp;
        StreamReader _reader;
        StreamWriter _writer;

        // Spielzustand
        string _serverWord = "";
        int _currentRow = 0;
        int[] _stats = new int[7]; // [0-5] = Versuch 1-6 erraten, [6] = nie erraten
        ObservableCollection<Cell> _cells = new();

        public MainWindow()
        {
            InitializeComponent();

            // 42 leere Zellen erstellen (6 Zeilen × 7 Spalten)
            for (int i = 0; i < 42; i++) _cells.Add(new Cell());
            GuessGrid.ItemsSource = _cells;

            // Mit Server verbinden
            _tcp = new TcpClient("127.0.0.1", 12345);
            var stream = _tcp.GetStream();
            _reader = new StreamReader(stream);
            _writer = new StreamWriter(stream) { AutoFlush = true };

            // Wort vom Server empfangen
            var xml = _reader.ReadLine();                     // "<Word>HOLIDAY</Word>"
            _serverWord = XDocument.Parse(xml).Root.Value;    // → "HOLIDAY"
        }

        // AUFGABE 3+4: Button Click
        private void RatenButton_Click(object sender, RoutedEventArgs e)
        {
            var guess = GuessInput.Text.ToUpper().Trim();

            // AUFGABE 4: Validierung – muss 7 Zeichen haben
            if (guess.Length != 7)
            {
                MessageBox.Show("Das Wort muss genau 7 Zeichen haben!", "Fehler");
                return;
            }

            // Guess zum Server schicken
            _writer.WriteLine($"<Guess>{guess}</Guess>");

            // Ergebnis empfangen
            var resultXml = _reader.ReadLine();               // "<Result>G,X,Y,G,X,X,G</Result>"
            var colors = XDocument.Parse(resultXml).Root.Value.Split(',');

            // AUFGABE 5: Grid einfärben
            ColorRow(_currentRow, guess, colors);
            _currentRow++;

            // AUFGABE 6: Gewonnen oder verloren prüfen
            if (colors.All(c => c == "G"))
            {
                EndGame(true);
            }
            else if (_currentRow >= 6)
            {
                EndGame(false);
            }

            GuessInput.Clear();
        }

        // AUFGABE 5: Zeile einfärben
        void ColorRow(int row, string guess, string[] colors)
        {
            for (int i = 0; i < 7; i++)
            {
                _cells[row * 7 + i].Letter = guess[i].ToString();
                _cells[row * 7 + i].Color = colors[i] switch
                {
                    "G" => Brushes.Green,
                    "Y" => Brushes.Yellow,
                    _ => Brushes.Gray
                };
            }
        }

        // AUFGABE 7: Spiel beenden
        void EndGame(bool won)
        {
            // Statistik updaten
            if (won) _stats[_currentRow - 1]++;
            else _stats[6]++;

            // Eingabe sperren
            GuessInput.IsEnabled = false;
            RatenButton.IsEnabled = false;

            // Fenster verbreitern
            this.Width *= 2;

            // Rechte Spalte aufmachen
            var grid = (Grid)this.Content;
            grid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);

            // Statistik einblenden
            StatsPanel.Visibility = Visibility.Visible;
            WordLabel.Content = $"Das Wort war: {_serverWord}";
            Stat1.Content = $"Beim 1. Versuch: {_stats[0]}";
            Stat2.Content = $"Beim 2. Versuch: {_stats[1]}";
            Stat3.Content = $"Beim 3. Versuch: {_stats[2]}";
            Stat4.Content = $"Beim 4. Versuch: {_stats[3]}";
            Stat5.Content = $"Beim 5. Versuch: {_stats[4]}";
            Stat6.Content = $"Beim 6. Versuch: {_stats[5]}";
            StatNever.Content = $"Nie erraten: {_stats[6]}";
        }
    }
}