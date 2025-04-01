namespace Barber.Maui.API.Models
{
    public class Cita
    {
        public long Id { get; set; }
        public string Nombre { get; set; }
        public string Telefono { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}
