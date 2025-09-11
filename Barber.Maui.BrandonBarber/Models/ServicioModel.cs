using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Barber.Maui.BrandonBarber.Models
{
    public class ServicioModel
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Imagen { get; set; }
        public decimal Precio { get; set; }
        public int IdBarberia { get; set; }
    }
}
