using System.ComponentModel.DataAnnotations.Schema;

namespace Barber.Maui.API.Models
{
    public class Cita
    {
        public int Id { get; set; }
        public long Cedula { get; set; }
        public string? Nombre { get; set; }
        public string? Telefono { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public long BarberoId { get; set; }
        [NotMapped]
        public string? BarberoNombre { get; set; }
        public string Estado { get; set; } = "Pendiente"; // Agregar esta línea
    }
}
