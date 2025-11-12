using System.Globalization;

namespace Barber.Maui.BrandonBarber.Converters
{
    public class LessThanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double calificacion && parameter is string param)
            {
                if (double.TryParse(param, out double threshold))
                {
                    return calificacion < threshold;
                }
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}