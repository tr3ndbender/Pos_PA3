using LinqToDB;
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

namespace WaldwunderDB
{
    /// <summary>didn
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<Waldwunder> WaldwunderItems { get; } = new();
        public static WaldwunderDB db = new WaldwunderDB(new DataOptions().UseSQLite(@"Data Source=./Waldwunder.db"));


        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            foreach (var x in db.Waldwunders)
            {
                WaldwunderItems.Add(x);
            }

            // Die nächsten Beiden Lines sind so Schwarze Magie für mich, ich check so was sie machen aber gleichzeitig könnte ich das nie im Leben nachprogrammieren.
            Map.SizeChanged += (_, __) => MarkWonders();
            Map.Loaded += (_, __) => MarkWonders();
        }

        private void WaldwunderButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new NewWaldwunder();

            bool? result = window.ShowDialog();

            if (result == true)
            {
                db.Insert(window.Result);
            }
        }

        private void AnzeigenButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = Waldwunder.SelectedItem as Waldwunder;

            if (selected != null)
            {
                WaldwunderAnzeigen window = new WaldwunderAnzeigen(selected);
                window.Show();
            }
        }
        

        //Region dafür, dass man Super hier den Code collapsen kann, weil der eh nicht juckt und halt nur da ist
        #region Map Marking
        private void MarkWonders()
        {
            Map.Children.Clear();

            double west = 9.362383;
            double east = 17.231941;
            double north = 49.063175;
            double south = 46.308597;

            double w = Map.ActualWidth;
            double h = Map.ActualHeight;

            foreach (var wonder in WaldwunderItems)
            {
                double lon;
                double lat;

                lon = (double)wonder.Longitude;
                lat = (double)wonder.Latitude;

                double xPos = (lon - west) / (east - west) * w;
                double yPos = ((north - lat) / (north - south)) * h;

                var marker = new System.Windows.Shapes.Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = System.Windows.Media.Brushes.Red,
                    Stroke = System.Windows.Media.Brushes.White,
                    StrokeThickness = 1,
                    ToolTip = wonder.Name
                };

                Canvas.SetLeft(marker, xPos - 5);
                Canvas.SetTop(marker, yPos - 5);

                Map.Children.Add(marker);
            }
        }
        #endregion
        // yallah


    }
}