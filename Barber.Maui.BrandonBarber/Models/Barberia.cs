namespace Barber.Maui.BrandonBarber.Models
{
    public class Barberia
    {
        public int Idbarberia { get; set; }
        public long Idadministrador { get; set; }
        public string? Nombre { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public string? Email { get; set; }

        public string? LogoUrl { get; set; }
        public bool IsSelected { get; set; }

        public List<string> ServiciosDestacados { get; set; } = [];

        // Nueva propiedad para mostrar el nombre del administrador
        public string? NombreAdministrador { get; set; }
    }
}