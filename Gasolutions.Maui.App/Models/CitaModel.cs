using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    }

}
