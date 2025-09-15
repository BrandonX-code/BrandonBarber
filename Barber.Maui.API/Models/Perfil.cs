using System.ComponentModel.DataAnnotations;

namespace Barber.Maui.API.Models
{
    public class Perfil
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public long Cedula { get; set; }

        [Required]
        [StringLength(100)]
        public string? Nombre { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(200)]
        public string? Direccion { get; set; }

        [StringLength(255)]
        public string? ImagenPath { get; set; }
    }
}