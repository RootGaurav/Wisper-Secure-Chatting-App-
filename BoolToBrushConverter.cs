using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Connectt
{
    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isOnline = (bool)value;
            return isOnline
                ? new SolidColorBrush(Color.FromRgb(46, 204, 113))  // Green
                : new SolidColorBrush(Color.FromRgb(231, 76, 60));  // Red
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
