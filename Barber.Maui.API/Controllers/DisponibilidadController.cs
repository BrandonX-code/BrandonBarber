using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Barber.Maui.API.Controllers
{
    [Route("api/disponibilidad")]
    [ApiController]
    public class DisponibilidadController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DisponibilidadController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Disponibilidad>>> GetDisponibilidad()
        {
            return await _context.Disponibilidad.ToListAsync();
        }

        [HttpGet("by-date/{fecha}/{barberoId}")]
        public async Task<ActionResult<Disponibilidad>> GetDisponibilidadPorFecha(DateTime fecha, int barberoId)
        {
            var disponibilidad = await _context.Disponibilidad
                .Where(d => d.Fecha.Date == fecha.Date && d.BarberoId == barberoId)
                .FirstOrDefaultAsync();

            if (disponibilidad == null)
            {
                return NotFound();
            }

            return Ok(disponibilidad);
        }

        [HttpPost]
        public async Task<ActionResult<Disponibilidad>> CrearDisponibilidad([FromBody] Disponibilidad nuevaDisponibilidad)
        {
            if (nuevaDisponibilidad == null || nuevaDisponibilidad.BarberoId <= 0)
            {
                return BadRequest(new { message = "Error: Datos inválidos en la solicitud." });
            }

            try
            {
                // Asegurar que la fecha solo tenga parte de día
                nuevaDisponibilidad.Fecha = nuevaDisponibilidad.Fecha.Date;

                // Buscar si ya existe una disponibilidad para esa fecha y barbero
                var disponibilidadExistente = await _context.Disponibilidad
                    .FirstOrDefaultAsync(d => d.Fecha == nuevaDisponibilidad.Fecha && d.BarberoId == nuevaDisponibilidad.BarberoId);

                if (disponibilidadExistente != null)
                {
                    // Actualizar los horarios existentes
                    disponibilidadExistente.Horarios = nuevaDisponibilidad.Horarios;
                    _context.Disponibilidad.Update(disponibilidadExistente);
                }
                else
                {
                    // Nueva disponibilidad
                    nuevaDisponibilidad.Id = 0; // Forzar creación de nuevo Id
                    _context.Disponibilidad.Add(nuevaDisponibilidad);
                }

                await _context.SaveChangesAsync();
                return StatusCode(201);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al guardar la disponibilidad.", error = ex.Message });
            }
        }



        //private async Task CancelarCitasAfectadas(Disponibilidad disponibilidad)
        //{
        //    // Obtener todas las citas para la fecha
        //    var citas = await _context.Disponibilidad
        //        .Where(c => c.Fecha.Date == disponibilidad.Fecha.Date && c.BarberoId == disponibilidad.BarberoId)
        //        .ToListAsync();

        //    if (citas.Any())
        //    {
        //        var horariosDict = JsonSerializer.Deserialize<Dictionary<string, bool>>(disponibilidad.Horarios);

        //        // Verificar cada cita
        //        foreach (var cita in citas)
        //        {
        //            string horaKey = $"{cita.Fecha.Hour}:00";

        //            // Si el horario ahora no está disponible, cancelar la cita
        //            if (horariosDict.ContainsKey(horaKey) && !horariosDict[horaKey])
        //            {
        //                _context.Disponibilidad.Remove(cita);
        //            }
        //        }

        //        await _context.SaveChangesAsync();
        //    }
        //}
    }
}