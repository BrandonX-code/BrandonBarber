using System.Collections.Generic;

namespace Barber.Maui.BrandonBarber.Models
{
    public class DisponibilidadModel
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public long BarberoId { get; set; }
        public string Horarios { get; set; } = "{}";
        public Dictionary<string, bool> HorariosDict { get; set; } = new Dictionary<string, bool>();
    }
}