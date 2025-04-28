using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Barber.Maui.API.Models
{
    public class Disponibilidad
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        public int BarberoId { get; set; }

        [Required]
        public string Horarios { get; set; } = "{}";

        [NotMapped]
        public Dictionary<string, bool> HorariosDict
        {
            get
            {
                try
                {
                    return JsonSerializer.Deserialize<Dictionary<string, bool>>(Horarios) ?? new Dictionary<string, bool>();
                }
                catch
                {
                    return new Dictionary<string, bool>();
                }
            }
            set
            {
                Horarios = JsonSerializer.Serialize(value);
            }
        }
    }
}