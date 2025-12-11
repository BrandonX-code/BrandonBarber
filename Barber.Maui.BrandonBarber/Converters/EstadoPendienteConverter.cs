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

    // ✅ CONVERTIDOR PARA PERMITIR ELIMINAR SOLO CITAS PENDIENTES CON >24 HORAS
    public class PuedeEliminarCitaConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // value es la cita completa pasada desde el binding
            if (value is not CitaModel cita)
                return false;

            // ✅ Solo permitir eliminar si está en estado PENDIENTE
            if (!string.Equals(cita.Estado, "Pendiente", StringComparison.OrdinalIgnoreCase))
                return false;

            // ✅ Y faltan al menos 24 horas
            var horasRestantes = (cita.Fecha - DateTime.UtcNow).TotalHours;
            return horasRestantes >= 24;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // ✅ CONVERTIDOR PARA PERMITIR EDITAR SOLO CITAS PENDIENTES CON >24 HORAS
    public class PuedeEditarCitaConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not CitaModel cita)
                return false;

            // ✅ Solo permitir editar si está en estado PENDIENTE
            if (!string.Equals(cita.Estado, "Pendiente", StringComparison.OrdinalIgnoreCase))
                return false;

            // ✅ Y faltan al menos 24 horas
            var horasRestantes = (cita.Fecha - DateTime.UtcNow).TotalHours;
            return horasRestantes >= 24;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}