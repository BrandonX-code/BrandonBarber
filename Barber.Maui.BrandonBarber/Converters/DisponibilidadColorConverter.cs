using System.Globalization;

namespace Barber.Maui.BrandonBarber.Converters
{
    public class DisponibilidadColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool disponible)
            {
                return disponible ? Color.FromArgb("#E0F7FA") : Color.FromArgb("#717d7e");
            }
            return Color.FromArgb("#E0F7FA");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
