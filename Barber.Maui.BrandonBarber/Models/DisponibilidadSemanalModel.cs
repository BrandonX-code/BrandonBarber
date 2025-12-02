using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Barber.Maui.BrandonBarber.Models
{
    public class DisponibilidadSemanalModel
    {
        public long BarberoId { get; set; }
        public List<DiaDisponibilidadModel> Dias { get; set; } = new();
    }

    public class DiaDisponibilidadModel
    {
        public string NombreDia { get; set; } = string.Empty; // "Lunes", "Martes", etc.
        public bool Habilitado { get; set; }
        public TimeSpan HoraInicio { get; set; } = new TimeSpan(9, 0, 0); // 9:00 AM
        public TimeSpan HoraFin { get; set; } = new TimeSpan(18, 0, 0); // 6:00 PM
    }
}
