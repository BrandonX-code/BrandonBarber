using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gasolutions.Maui.App.Models
{
    public class CalificacionModel
    {
        public long BarberoId { get; set; }
        public long ClienteId { get; set; }
        public int Puntuacion { get; set; }
        public string? Comentario { get; set; }
        public DateTime FechaCalificacion { get; set; }
    }
}
