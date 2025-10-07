using System.Globalization;

namespace Barber.Maui.BrandonBarber.style
{
    public class EstadoColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string estado)
            {
                return estado.ToLower() switch
                {
                    "completada" => Color.FromArgb("#4CAF50"), // Verde
                    "cancelada" => Color.FromArgb("#F44336"),  // Rojo
                    _ => Color.FromArgb("#FFA726")             // Naranja para pendiente
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