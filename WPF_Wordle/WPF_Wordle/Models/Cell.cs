using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using System.Text;

namespace WPF_Wordle.Models
{
    public class Cell : INotifyPropertyChanged
    {
        private string _letter = "";
        private Brush _color = Brushes.LightGray;

        public string Letter
        {
            get => _letter;
            set { _letter = value; OnPropertyChanged(); }
        }
        public Brush Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? prop = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
