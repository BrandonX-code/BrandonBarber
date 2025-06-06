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

        [HttpGet("by-date/{fecha}")]
        public async Task<ActionResult<Disponibilidad>> GetDisponibilidadPorFecha(DateTime fecha)
        {
            var disponibilidad = await _context.Disponibilidad
                .Where(d => d.Fecha.Date == fecha.Date)
                .FirstOrDefaultAsync();

            if (disponibilidad == null)
            {
                return NotFound();
            }

            return Ok(disponibilidad);
        }

        [HttpGet("by-barberId/{cedula}")]
        public async Task<ActionResult<Disponibilidad>> GetDisponibilidadPorBarberoId(long cedula)
        {
            var fecha = DateTime.Now;
            var disponibilidad = await _context.Disponibilidad
                .Where(d => d.Fecha.Date == fecha.Date).Where(d => d.BarberoId == cedula)
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
    }
}