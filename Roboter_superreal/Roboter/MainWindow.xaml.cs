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

namespace Roboter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Regex regex = new Regex(@"REPEAT|MOVE|COLLECT|LEFT|RIGHT|UP|DOWN|\d+|{|}|\S+");
        private Regex numberRegex = new Regex(@"\d+");
        private Regex keywordRegex = new Regex(@"REPEAT|MOVE|COLLECT");
        private Regex directionRegex = new Regex(@"LEFT|RIGHT|UP|DOWN");

        private List<Token> tokens = new List<Token>();

        public MainWindow()
        {
            InitializeComponent();

            Field.LoadField("Aufgabe1.xml");

            Code.Text = "REPEAT 2 {\r\n    MOVE RIGHT\r\n}\r\nREPEAT 6 {\r\n    MOVE DOWN\r\n}\r\nREPEAT 2 {\r\n    MOVE LEFT\r\n}\r\nCOLLECT\r\nREPEAT 4 {\r\n    MOVE RIGHT\r\n}\r\nMOVE DOWN\r\nCOLLECT\r\nMOVE RIGHT\r\nREPEAT 4 {\r\n    MOVE UP\r\n}\r\nMOVE LEFT\r\nCOLLECT";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // 1. Schritt: Tokenisieren
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

                    case "{":
                        token.Type = Token.TokenType.OpenBracket;
                        break;

                    case "}":
                        token.Type = Token.TokenType.CloseBracket;
                        break;
                }
            }
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
            Programm programm = new();
            programm.Parse(tokens);

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

            ThreadPool.QueueUserWorkItem(_ =>
                {
                    // Schritt 3: Ausführen
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
                });


        }
    }
}