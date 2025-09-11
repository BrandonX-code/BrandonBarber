using System.ComponentModel.DataAnnotations;

namespace Barber.Maui.BrandonBarber.Models
{
    public class ImagenGaleriaModel
    {
        public int Id { get; set; }

        [Required]
        public string? NombreArchivo { get; set; }

        [Required]
        public string? RutaArchivo { get; set; }

        public string? Descripcion { get; set; }

        public string TipoImagen { get; set; } = "jpg";

        public long? TamanoBytes { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaModificacion { get; set; }

        public bool Activo { get; set; } = true;
    }
}