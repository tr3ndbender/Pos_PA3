using LinqToDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WaldwunderDB
{
    /// <summary>
    /// Interaction logic for WaldwunderAnzeigen.xaml
    /// </summary>
    public partial class WaldwunderAnzeigen : Window
    {
        private List<Bilder> _bilder;
        private int _currentImageIndex = 0;

        public WaldwunderAnzeigen(Waldwunder waldwunder)
        {
            InitializeComponent();
            DataContext = waldwunder;

            _bilder = MainWindow.db.Bilders
                .Where(b => b.Wonder == waldwunder.Id)
                .ToList();

            if (_bilder.Any())
            {
                ShowImage(_currentImageIndex);
            }
        }

        private void ShowImage(int index)
        {
            WaldwunderBild.Source = new BitmapImage(
                new Uri($"Bilder/{_bilder[index].Name}", UriKind.Relative)
            );
        }

        private void NaechstesBild_Click(object sender, RoutedEventArgs e)
        {
            if (!_bilder.Any())
                return;

            _currentImageIndex++;

            if (_currentImageIndex >= _bilder.Count)
                _currentImageIndex = 0; // start again at first image

            ShowImage(_currentImageIndex);
        }
    }
}
