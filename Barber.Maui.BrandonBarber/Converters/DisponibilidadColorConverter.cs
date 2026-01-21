using System.Globalization;

namespace Barber.Maui.BrandonBarber.Converters
{
    public class DisponibilidadColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool disponible)
            {
                return disponible ? Color.FromArgb("#265d82") : Color.FromArgb("#717d7e");
            }
            return Color.FromArgb("#265d82");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
