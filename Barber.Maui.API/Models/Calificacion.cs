using System.ComponentModel.DataAnnotations;
namespace Barber.Maui.API.Models
{
    public class Calificacion
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public long BarberoId { get; set; }
        [Required]
        public long ClienteId { get; set; }
        [Required]
        [Range(1, 5)]
        public int Puntuacion { get; set; }
        public string? Comentario { get; set; }
        public DateTime FechaCalificacion { get; set; }
    }
}
