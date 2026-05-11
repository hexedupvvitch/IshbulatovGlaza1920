using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IshbulatovGlaza1920
{
    public class DiscountToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int discount && discount >= 25)
            {
                return new SolidColorBrush(Colors.LightGreen);
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}