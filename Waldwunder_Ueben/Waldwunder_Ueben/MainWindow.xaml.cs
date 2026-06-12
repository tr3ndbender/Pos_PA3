using DataModels;
using LinqToDB;
using LinqToDB.Async;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

namespace Waldwunder_Ueben
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<Waldwunder> waldwunders { get; set; } = new(); //Get und set wichtig
        

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
            var options = new DataOptions().UseSQLite(@"Data Source=Waldwunder.db");
            using var db = new WaldwunderDB(new DataOptions<WaldwunderDB>(options));

            //Hier Hier alles mit Join von Bildern
            var alleMitBild = db.GetTable<Waldwunder>()
                       .LoadWith(w => w.Bilders) // linq2db automatically handles the JOIN to the Bilder table!
                       .ToList();

            var alle = db.Waldwunders.Select(w => w);

            alleMitBild.ForEach(w =>
            {
                waldwunders.Add(w);
                Debug.WriteLine(w.Bilders.ToList()[0].NameBild);
                // Struktur der Daten gerade (ID, Name, ..., [Liste mit Bild Data Model])
            });


        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Waldwunder selected = (wunderListBox.SelectedItem as Waldwunder);
            if (selected == null) return;

            Dispatcher.BeginInvoke(() =>
            {
                DetailName.Text = selected.Name;
                Description.Text = selected.Description;
                Province.Text = selected.Province;
                Latitude.Text = selected.Latitude.ToString();
                Longitude.Text = selected.Longitude.ToString();
                Votes.Text = selected.Votes.ToString();

                // Alle Bilder des ausgewählten Waldes laden
                BilderItems.ItemsSource = LadeBilder(selected);

                mapCanvas.Visibility = Visibility.Collapsed;
                detailStackPanel.Visibility = Visibility.Visible;
            });
        }

        // Sucht im Bilder-Ordner alle Dateien, deren Basisname zu den DB-Einträgen des Waldes passt.
        private static List<BitmapImage> LadeBilder(Waldwunder wald)
        {
            var ergebnis = new List<BitmapImage>();
            string ordner = System.IO.Path.Combine(AppContext.BaseDirectory, "Bilder");
            if (!System.IO.Directory.Exists(ordner)) return ergebnis;

            // Aus allen verknüpften Namen die eindeutigen Basisnamen bilden ("Ahornriesen1.jpg" -> "Ahornriesen")
            var basisNamen = wald.Bilders
                .Where(b => b.NameBild != null)
                .Select(b => BasisName(b.NameBild!))
                .Distinct();

            foreach (string basis in basisNamen)
            {
                // alle Dateien, die mit dem Basisnamen beginnen: NameZahl.ext
                foreach (string datei in System.IO.Directory.GetFiles(ordner, basis + "*").OrderBy(p => p))
                {
                    ergebnis.Add(new BitmapImage(new Uri(datei)));
                }
            }
            return ergebnis;
        }

        // Endung weg, dann angehängte Ziffern abschneiden: "Ahornriesen1.jpg" -> "Ahornriesen", "Wald.png" -> "Wald"
        private static string BasisName(string dateiname)
        {
            string ohneEndung = System.IO.Path.GetFileNameWithoutExtension(dateiname);
            return ohneEndung.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
        }
    }
}