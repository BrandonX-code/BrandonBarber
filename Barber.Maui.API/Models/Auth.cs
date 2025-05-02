using System.ComponentModel.DataAnnotations;

namespace Barber.Maui.API.Models
{
    public class Auth
    {
        [Key]
        [Required]
        public long Cedula { get; set; }

        [StringLength(100)] // Limitar el nombre a 100 caracteres
        public string Nombre { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
        [StringLength(100)] // Limitar el email a 100 caracteres
        public string Email { get; set; }

        [StringLength(250)] // Limitar la dirección a 250 caracteres
        public string Direccion { get; set; }

        public string Telefono { get; set; }  // Cambiar a string

        [Required]
        [MinLength(6)]
        public string Contraseña { get; set; }

        [StringLength(50)] // Limitar el rol a 50 caracteres
        public string Rol { get; set; }
    }
}
