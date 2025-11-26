using System.ComponentModel.DataAnnotations;

namespace Barber.Maui.API.Models
{
    public class FcmToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public long UsuarioCedula { get; set; }

        [Required]
        [MaxLength(500)]
        public string Token { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        public DateTime? UltimaActualizacion { get; set; }
    }
}