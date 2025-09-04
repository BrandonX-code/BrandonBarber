using System.Text.Json.Serialization;

namespace Barber.Maui.BrandonBarber.Models
{
    public class PerfilUsuario
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("cedula")]
        public long Cedula { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; }

        [JsonPropertyName("telefono")]
        public string Telefono { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("direccion")]
        public string Direccion { get; set; }

        [JsonPropertyName("imagenPath")]
        public string ImagenPath { get; set; }
    }
}