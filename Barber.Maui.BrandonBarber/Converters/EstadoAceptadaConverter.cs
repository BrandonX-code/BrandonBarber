using System.Globalization;

namespace Barber.Maui.BrandonBarber.Converters
{
    public class EstadoAceptadaConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var estado = value?.ToString()?.ToLower();

            return estado == "completada" || estado == "aceptada";
        }


        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}