namespace Gasolutions.Maui.App.Models
{
    public class Barberia
    {
        public int Idbarberia { get; set; }
        public long Idadministrador { get; set; }
        public string? Nombre { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public string? Email { get; set; }

        // Propiedades para UI
        public string? LogoUrl { get; set; } // URL o nombre del archivo local
        public bool IsSelected { get; set; }

        // Puedes agregar servicios destacados si los tienes
        public List<string> ServiciosDestacados { get; set; } = new();
    }
}