using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Austrian_Painter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Der "große" Regex zerlegt den Text in einzelne Tokens:
        private Regex regex = new Regex(@"TURN|COLOR|DRAW|FOR|LEFT|RIGHT|\d+|{|}|\S+");

        // Die "kleinen" Regex bestimmen, WAS ein Token ist:
        private Regex numberRegex = new Regex(@"^\d+$");
        private Regex keywordRegex = new Regex(@"^(TURN|COLOR|DRAW|FOR)$");
        private Regex directionRegex = new Regex(@"^(LEFT|RIGHT)$");
        private Regex colorRegex = new Regex(@"^(White|Black|Red|Green|Blue|Yellow|Orange|Purple|Pink|Gray|Brown|Cyan|Magenta)$");


        private List<Token> tokens = new List<Token>();
        
        public MainWindow()
        {
            InitializeComponent();

            // Beispiel-Code aus der Angabe vorbefüllen
            Code.Text =
                "TURN RIGHT 45\r\n" +
                "COLOR White\r\n" +
                "DRAW 250\r\n" +
                "FOR 6 {\r\n" +
                "COLOR Red\r\n" +
                "TURN LEFT 150\r\n" +
                "DRAW 150\r\n" +
                "COLOR Blue\r\n" +
                "TURN LEFT 150\r\n" +
                "DRAW 150\r\n" +
                "}\r\n" +
                "TURN RIGHT 90\r\n" +
                "COLOR Green\r\n" +
                "FOR 12 {\r\n" +
                "TURN RIGHT 30\r\n" +
                "DRAW 40\r\n" +
                "}";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            tokens.Clear();

            foreach (Match match in regex.Matches(Code.Text))
            {
                Token token = new Token() { Value = match.Value };
                tokens.Add(token);
                switch (match.Value)
                {
                    case var _ when numberRegex.IsMatch(match.Value):
                        token.Type = Token.TokenType.Number;
                        break;

                    case var _ when keywordRegex.IsMatch(match.Value):
                        token.Type = Token.TokenType.Keyword;
                        break;

                    case var _ when directionRegex.IsMatch(match.Value):
                        token.Type = Token.TokenType.Direction;
                        break;

                    case var _ when colorRegex.IsMatch(match.Value):
                        token.Type = Token.TokenType.Color;
                        break;

                    case "{":
                        token.Type = Token.TokenType.OpenBracket;
                        break;

                    case "}":
                        token.Type = Token.TokenType.CloseBracket;
                        break;
                }
            }
            TokensList.ItemsSource = null;
            TokensList.ItemsSource = tokens;

            //Schritt 1.5: Fehlerhafte Tokens ausgeben
            var errors = tokens.Where(t => t.Type == Token.TokenType.Error).ToList();
            if (errors.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Fehlerhafte Tokens:");
                foreach (var error in errors)
                {
                    sb.AppendLine(error.Value);
                }
                MessageBox.Show(sb.ToString(), "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            //Schritt 2: Parsen
            // Eine Kopie parsen, damit die angezeigte Token-Liste erhalten bleibt
            // (der Parser entfernt die Tokens beim Verarbeiten mit RemoveAt).
            Programm programm = new();
            programm.Parse(new List<Token>(tokens));

            //Schritt 2.5: Fehlerhafte Anweisungen ausgeben
            if (Expression.Errors.Count > 0)
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("Fehlerhafte Anweisungen:");
                foreach (var error in Expression.Errors)
                {
                    builder.AppendLine(error);
                }
                MessageBox.Show(builder.ToString(), "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Expression.Errors.Clear();

            //Schritt 3: Ausführen
            // Das PainterControl zeichnet direkt auf seine Canvas, deshalb muss
            // die Ausführung im UI-Thread laufen (kein ThreadPool wie beim Roboter).
            Field.Clear();
            programm.Run(Field);

            //Schritt 3.5: Fehlerhafte Ausführung ausgeben
            if (Expression.Errors.Count > 0)
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("Fehlerhafte Ausführung:");
                foreach (var error in Expression.Errors)
                {
                    builder.AppendLine(error);
                }
                MessageBox.Show(builder.ToString(), "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Expression.Errors.Clear();
        }
    }
}