using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Barber.Maui.API.Models
{
    public class ImagenGaleria
    {
        [Key]
        public int Id { get; set; }

        public long BarberoId { get; set; }

        [Required]
        [MaxLength(255)]
        public string? NombreArchivo { get; set; }

        [Required]
        [MaxLength(500)]
        public string? RutaArchivo { get; set; }

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        [Required]
        [MaxLength(50)]
        public string TipoImagen { get; set; } = "jpg";

        public long? TamanoBytes { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaModificacion { get; set; }

        [Required]
        public bool Activo { get; set; } = true;
    }
}