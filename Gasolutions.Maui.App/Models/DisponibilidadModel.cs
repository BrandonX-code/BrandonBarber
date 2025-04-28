using System.Collections.Generic;

namespace Gasolutions.Maui.App.Models
{
    public class DisponibilidadModel
    {
        public int Id { get; set; } = 0;
        public DateTime Fecha { get; set; }
        public int BarberoId { get; set; }
        public Dictionary<string, bool> Horarios { get; set; } = new Dictionary<string, bool>();
    }
}