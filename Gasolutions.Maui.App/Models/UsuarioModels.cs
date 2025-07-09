using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Gasolutions.Maui.App.Models
{
    public class UsuarioModels
    {
        public int Id { get; set; }
        [Required]
        public long Cedula { get; set; }
        public string Nombre { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [MinLength(6)]
        public string Contraseña { get; set; }
        public string Rol { get; set; } // cliente, barbero, administrador
        public string Token { get; set; }
        public string Telefono { get; set; }

        [JsonPropertyName("imagenPath")]
        public string? ImagenPath { get; set; }

        [StringLength(200)]
        public string Direccion { get; set; }

        [JsonIgnore]
        public bool IsAdmin => Rol?.ToLower() == "admin";

        [JsonIgnore]
        public bool IsBarbero => Rol?.ToLower() == "barbero";

        [JsonIgnore]
        public bool IsCliente => Rol?.ToLower() == "cliente";

        public int BarberoId { get; set; }
        public int Visitas { get; set; }

        public string Especialidades { get; set; }
    }

    // Clase para el login
    public class LoginRequest
    {
        [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
        public string Email { get; set; }
        public string Contraseña { get; set; }
    }

    // Clase para el registro
    public class RegistroRequest
    {
        public long Cedula { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Contraseña { get; set; }
        public string ConfirmContraseña { get; set; }
        public string Telefono { get; set; }
        public string Direccion { get; set; }   
        public string Rol { get; set; } // cliente, barbero, administrador

        public string Especialidades { get; set; }
    }

    // Clase para la respuesta de autenticación
    public class AuthResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public UsuarioModels User { get; set; }
        public string Token { get; set; }
        //public bool IsSuccess { get; internal set; }
    }
}