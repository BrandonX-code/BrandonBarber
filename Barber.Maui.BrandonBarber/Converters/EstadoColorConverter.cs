using System.Globalization;

namespace Barber.Maui.BrandonBarber.Converters
{
    public class EstadoColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string estado)
            {
                return estado.ToLower() switch
                {
                    // ✅ Aceptar "confirmada" o "completada" como equivalentes
                    "confirmada" or "completada" or "aprobado" => Color.FromArgb("#4CAF50"),
                    "cancelada" or "rechazado" => Color.FromArgb("#F44336"),
                    "pendiente" => Color.FromArgb("#FFA726"),
                    "finalizada" => Color.FromArgb("#4D5154"), // 👈 AÑADIDO
                    "reagendarpendiente" => Color.FromArgb("#FF6F91"),
                    _ => Color.FromArgb("#90A4AE")
                };

            }
            return Color.FromArgb("#90A4AE");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}