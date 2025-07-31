using System.ComponentModel.DataAnnotations;

namespace Barber.Maui.API.Models
{
    public class Barberia
    {
        [Key]
        public int Idbarberia { get; set; }

        [Required]
        public long Idadministrador { get; set; }

        public string? Nombre { get; set; }

        public string? Telefono { get; set; }

        public string? Direccion { get; set; }

        public string? Email { get; set; }
        public string? LogoUrl { get; internal set; }
    }
}
