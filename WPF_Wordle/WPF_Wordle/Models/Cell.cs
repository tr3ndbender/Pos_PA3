using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using System.Text;

namespace WPF_Wordle.Models
{
    class Cell : INotifyPropertyChanged
    {
        private string _letter = "";
        private Brush _color = Brushes.LightGray;

        public string Letter
        {
            get => _letter;
            set { _letter = value; OnPropertyChanged(nameof(Letter)); }
        }
        public Brush Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(nameof(Color)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
