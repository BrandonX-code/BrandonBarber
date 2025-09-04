using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Barber.Maui.BrandonBarber.style
{
    public class DisponibilidadColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string disponibilidad)
            {
                return disponibilidad.ToLower() switch
                {
                    "disponible" => Colors.LightGreen,
                    "no disponible" => Colors.IndianRed,
                    _ => Colors.Gray
                };
            }
            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
