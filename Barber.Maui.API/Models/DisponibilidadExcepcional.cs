using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Barber.Maui.API.Models
{
    /// <summary>
    /// Representa excepciones en la disponibilidad del barbero
    /// (días libres, cambios de horario, etc.)
    /// </summary>
    [Table("DisponibilidadesExcepcionales")]
    public class DisponibilidadExcepcional
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public long BarberoId { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        /// <summary>
        /// Tipo de excepción: "DiaCompleto", "HorarioModificado"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string TipoExcepcion { get; set; } = string.Empty;

        /// <summary>
        /// Motivo de la excepción (opcional)
        /// </summary>
        [MaxLength(500)]
        public string? Motivo { get; set; }

        /// <summary>
        /// Horarios disponibles si es HorarioModificado (JSON)
        /// Formato: {"09:00 - 09:40": true, "10:00 - 10:40": true}
        /// </summary>
        public string? HorariosModificados { get; set; }

        /// <summary>
        /// Si es true, el barbero no está disponible en absoluto ese día
        /// </summary>
        public bool DiaCompleto { get; set; }

        /// <summary>
        /// Fecha de creación de la excepción
        /// </summary>
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Si se notificó a los clientes afectados
        /// </summary>
        public bool ClientesNotificados { get; set; } = false;

        /// <summary>
        /// IDs de las citas afectadas (separadas por coma)
        /// </summary>
        public string? CitasAfectadas { get; set; }
    }
}