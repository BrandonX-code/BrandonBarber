using System.Globalization;

namespace Barber.Maui.BrandonBarber.Converters
{
    public class EstadoPendienteConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? estado = value?.ToString();
            return string.Equals(estado, "Pendiente", StringComparison.OrdinalIgnoreCase);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // ✅ NUEVO CONVERTIDOR PARA PERMITIR ELIMINAR TODOS EXCEPTO FINALIZADA
    public class EstadoEliminableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? estado = value?.ToString();
            // Permitir eliminar TODO EXCEPTO "Finalizada"
            return !string.Equals(estado, "Finalizada", StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}