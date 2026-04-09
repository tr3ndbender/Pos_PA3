using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfChat.Client.Converters
{
    public class UnreadColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (bool)value ? Brushes.OrangeRed : Brushes.Black;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
