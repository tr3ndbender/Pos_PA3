using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Client
{
    public class Cell : INotifyPropertyChanged
    {
        private char _buchstabe;
        private Brush _farbe = Brushes.White;

        public char Buchstabe
        {
            get => _buchstabe;
            set { _buchstabe = value; OnPropertyChanged(); }
        }

        public Brush Farbe
        {
            get => _farbe;
            set { _farbe = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
