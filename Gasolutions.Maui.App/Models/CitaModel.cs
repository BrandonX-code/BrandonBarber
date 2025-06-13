namespace Gasolutions.Maui.App.Models
{
    public class CitaModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Telefono { get; set; }
        public DateTime Fecha { get; set; }
        public string FechaConFormato => Fecha.ToString("dd/MM/yyyy hh:mm tt");
        public bool Seleccionado { get; set; }
        public long Cedula { get; set; }
        public bool EsHoy => Fecha.Date == DateTime.Today;
        public long BarberoId { get; set; }

        public string Estado
        {
            get
            {
                if (Fecha < DateTime.Now)
                    return "Pasada";
                else if (Fecha.Date == DateTime.Today)
                    return "Hoy";
                else
                    return "Futura";
            }
        }
    }

}
