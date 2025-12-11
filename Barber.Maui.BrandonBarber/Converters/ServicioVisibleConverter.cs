using System.Globalization;

namespace Barber.Maui.BrandonBarber.Converters
{
    public class ServicioVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CitaModel cita)
            {
                // ✅ SIMPLIFICAR: Solo verificar que ServicioNombre no esté vacío
                return !string.IsNullOrWhiteSpace(cita.ServicioNombre);
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}
