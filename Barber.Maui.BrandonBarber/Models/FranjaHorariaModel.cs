using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Barber.Maui.BrandonBarber.Models
{
    public class FranjaHorariaModel
    {
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public bool EstaDisponible { get; set; }
        public string HoraTexto
        {
            get
            {
                var inicio = DateTime.Today.Add(HoraInicio);
                var fin = DateTime.Today.Add(HoraFin);

                return $"{inicio:hh:mm tt} - {fin:hh:mm tt}";
            }
        }

    }
}
