using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Barber.Maui.API.Models
{
    [Table("SolicitudesAdmin")]
    public class SolicitudAdmin
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public long CedulaSolicitante { get; set; }

        [Required]
        [StringLength(100)]
        public string? NombreSolicitante { get; set; }

        [Required]
        [EmailAddress]
        public string? EmailSolicitante { get; set; }

        [StringLength(20)]
        public string? TelefonoSolicitante { get; set; }

        [Required]
        [StringLength(500)]
        public string? Justificacion { get; set; }

        [StringLength(20)]
        public string Estado { get; set; } = "Pendiente"; // Pendiente, Aprobado, Rechazado

        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        public DateTime? FechaRespuesta { get; set; }

        public long? CedulaRevisor { get; set; }

        [StringLength(200)]
        public string? MotivoRechazo { get; set; }
    }
}