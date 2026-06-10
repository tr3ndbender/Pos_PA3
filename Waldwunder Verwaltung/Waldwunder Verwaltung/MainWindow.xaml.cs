using Microsoft.Win32;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Collections.ObjectModel;
using DataModel;
using LinqToDB.Data;
using LinqToDB;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;

namespace Waldwunder_Verwaltung
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<Waldwunder> waldwunders = new();
        public WaldwunderDB db = new WaldwunderDB(new DataOptions().UseSQLite(@"Data Source=./Waldwunder.db"));

        public MainWindow()
        {
            InitializeComponent();

            foreach(var x in db.Waldwunders)
            {
                waldwunders.Add(x);
            }
            lbWaldwunder.ItemsSource = waldwunders;
        }

        private void MarkWonders()
        {
            mapCanvas.Children.Clear();

            double west = 9.362383;
            double east = 17.231941;
            double north = 49.063175;
            double south = 46.308597;

            double w = mapCanvas.ActualWidth;
            double h = mapCanvas.ActualHeight;

            foreach (var wonder in waldwunders)
            {
                double lon = 0.0;
                double lat = 0.0;

                lon = (double)wonder.Longitude.GetValueOrDefault();
                lat = (double)wonder.Latitude.GetValueOrDefault();

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

                mapCanvas.Children.Add(marker);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MarkWonders();
        }

        private void NewWonder_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            dialog.Visibility = Visibility.Visible;
            mapCanvas.Visibility = Visibility.Collapsed;
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            dialog.Visibility = Visibility.Collapsed;
            mapCanvas.Visibility = Visibility.Visible;

            Waldwunder waldwunder = new()
            {
                Name = tbName.Text,
                Description = tbDescription.Text,
                Province = tbProvince.Text,
                Type = tbType.Text,
            };

            try
            {
                waldwunder.Longitude = decimal.Parse(tbLongitude.Text);
                waldwunder.Latitude = decimal.Parse(tbLatitude.Text);
            }
            catch 
            { 
                // Fehler
            }
            db.Insert(waldwunder);
            waldwunders.Add(waldwunder);

            // Bildspeicherung
            foreach (string img in lbImages.Items)
            {
                Bilder bild = new()
                {
                    Name = img,
                    Wonder = waldwunder.Id
                };

                db.Insert(bild);
            }
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new() { Multiselect = true };
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (String filename in openFileDialog.FileNames)
                    lbImages.Items.Add(Path.GetFileName(filename));
            }
        }
        private void btnRemoveImage_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mapCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MarkWonders();
        }

        private void Anzeigen_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var selectedWonder = lbWaldwunder.SelectedItem as Waldwunder;

            if (selectedWonder != null) {
                showDialog.DataContext = selectedWonder;

                showDialog.Visibility = Visibility.Visible;
                mapCanvas.Visibility = Visibility.Collapsed;
                dialog.Visibility = Visibility.Collapsed;
            }
        }

        private void Anzeigen_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (lbWaldwunder?.SelectedItem == null || dialog?.Visibility == Visibility.Visible
                || showDialog?.Visibility == Visibility.Visible)
                e.CanExecute = false;
            else
                e.CanExecute = true;
        }

        private void NewWonder_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (dialog?.Visibility == Visibility.Visible)
                e.CanExecute = false;
            else
                e.CanExecute = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            dialog.Visibility = Visibility.Collapsed;
            showDialog.Visibility = Visibility.Collapsed;
            tbDescription.Text = tbName.Text = tbLatitude.Text = tbLongitude.Text = tbProvince.Text = tbType.Text = "";
            lbImages.Items.Clear();
            mapCanvas.Visibility = Visibility.Visible;
        }
    }
}