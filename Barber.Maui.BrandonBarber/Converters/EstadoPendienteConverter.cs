using System.Globalization;
using Barber.Maui.BrandonBarber.Models;

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

    // ✅ CONVERTIDOR PARA PERMITIR ELIMINAR SOLO CITAS PENDIENTES O CONFIRMADAS
    public class PuedeEliminarCitaConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // value es la cita completa pasada desde el binding
            if (value is not CitaModel cita)
                return false;

            // ✅ Permitir eliminar si está en estado PENDIENTE, CONFIRMADA o COMPLETADA
            return string.Equals(cita.Estado, "Pendiente", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(cita.Estado, "Confirmada", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(cita.Estado, "Completada", StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // ✅ CONVERTIDOR PARA PERMITIR EDITAR SOLO CITAS PENDIENTES O CONFIRMADAS
    public class PuedeEditarCitaConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not CitaModel cita)
                return false;

            // ✅ Permitir editar si está en estado PENDIENTE, CONFIRMADA o COMPLETADA
            return string.Equals(cita.Estado, "Pendiente", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(cita.Estado, "Confirmada", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(cita.Estado, "Completada", StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}