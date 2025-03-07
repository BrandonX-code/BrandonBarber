using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gasolutions.Maui.App.Models
{
    public class CitaModel
    {
        public long Id { get; set; }
        public string Nombre { get; set; }
        public string Telefono { get; set; }
        public DateTime Fecha { get; set; }
        public string FechaConFormato => Fecha.ToString("dd/MM/yyyy HH:mm tt");
    }

}
