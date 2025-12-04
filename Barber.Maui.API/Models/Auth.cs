using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Barber.Maui.API.Models
{
    public class Auth
    {
        public long Cedula { get; set; }
        public string? Nombre { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        [Column("contrasena")]
        public string? Contraseña { get; set; }

        public string? Rol { get; set; }
        public string? ImagenPath { get; set; }
        public string? Especialidades { get; set; }
        public int? IdBarberia { get; set; }

        // Add the missing properties to fix the error
        public double CalificacionPromedio { get; set; }
        public int TotalCalificaciones { get; set; }
    }
}
