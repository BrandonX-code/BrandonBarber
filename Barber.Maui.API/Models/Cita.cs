using System.ComponentModel.DataAnnotations.Schema;

namespace Barber.Maui.API.Models
{
    public class Cita
    {
        public int Id { get; set; }
        public long Cedula { get; set; }
        public string? Nombre { get; set; }
        public string? Telefono { get; set; }
        public DateTime Fecha { get; set; }
        public long BarberoId { get; set; }
        [NotMapped]
        public string? BarberoNombre { get; set; }
        public string Estado { get; set; } = "Pendiente"; // Agregar esta línea
        public int? ServicioId { get; set; }
        public string? ServicioNombre { get; set; }
        public decimal? ServicioPrecio { get; set; }
        // ✅ NUEVA PROPIEDAD: Imagen del servicio
        public string? ServicioImagen { get; set; }
    }
}
