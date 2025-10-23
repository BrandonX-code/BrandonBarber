namespace Barber.Maui.BrandonBarber.Models
{
    public class CitaModel
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Telefono { get; set; }
        public DateTime Fecha { get; set; }
        public string FechaConFormato => Fecha.ToString("dd/MM/yyyy hh:mm tt");
        public bool Seleccionado { get; set; }
        public long Cedula { get; set; }
        public bool EsHoy => Fecha.Date == DateTime.Today;
        public long BarberoId { get; set; }
        public string BarberoNombre { get; set; } = string.Empty;

        // Cambiar esta propiedad para que sea get y set
        public string Estado { get; set; } = "Pendiente";

        // Agregar esta nueva propiedad computada
        public string IconoEstado
        {
            get
            {
                return Estado?.ToLower() switch
                {
                    "completada" => "✓",
                    "cancelada" => "✗",
                    _ => "🟡"
                };
            }
        }

        public bool MostrarBarberoInfo { get; set; }
    }

}
