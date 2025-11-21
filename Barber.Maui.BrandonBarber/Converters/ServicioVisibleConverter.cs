using System.Globalization;

namespace Barber.Maui.BrandonBarber.Converters
{
    public class ServicioVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CitaModel cita)
            {
                return !string.IsNullOrWhiteSpace(cita.ServicioNombre)
                       && cita.ServicioPrecio > 0;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}
