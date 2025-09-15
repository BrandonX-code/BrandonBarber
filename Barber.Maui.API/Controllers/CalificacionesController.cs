using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Barber.Maui.API.Controllers
{
    [Route("api/calificaciones")]
    [ApiController]
    public class CalificacionesController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

        // POST: api/calificaciones
        [HttpPost]
        public async Task<IActionResult> Calificar([FromBody] Calificacion calificacion)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Buscar si ya existe una calificación para ese barbero y cliente
                var calificacionExistente = await _context.Calificaciones
                    .FirstOrDefaultAsync(c => c.BarberoId == calificacion.BarberoId && c.ClienteId == calificacion.ClienteId);

                if (calificacionExistente != null)
                {
                    // Actualizar la calificación existente
                    calificacionExistente.Puntuacion = calificacion.Puntuacion;
                    calificacionExistente.Comentario = calificacion.Comentario;
                    calificacionExistente.FechaCalificacion = DateTime.UtcNow;
                }
                else
                {
                    // Crear nueva calificación
                    calificacion.FechaCalificacion = DateTime.UtcNow;
                    _context.Calificaciones.Add(calificacion);
                }

                await _context.SaveChangesAsync();

                // Actualizar promedio en el perfil del barbero
                var calificaciones = await _context.Calificaciones
                    .Where(c => c.BarberoId == calificacion.BarberoId)
                    .ToListAsync();

                var promedio = calificaciones.Average(c => c.Puntuacion);
                var total = calificaciones.Count;

                var barbero = await _context.UsuarioPerfiles.FirstOrDefaultAsync(b => b.Cedula == calificacion.BarberoId);
                if (barbero != null)
                {
                    barbero.CalificacionPromedio = promedio;
                    barbero.TotalCalificaciones = total;
                    await _context.SaveChangesAsync();
                }

                return Ok(new { promedio, total });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message} - {ex.InnerException?.Message}");
            }
        }

        // GET: api/calificaciones/barbero/123456
        [HttpGet("barbero/{barberoId}")]
        public async Task<IActionResult> GetCalificacionesBarbero(long barberoId)
        {
            var calificaciones = await _context.Calificaciones
                .Where(c => c.BarberoId == barberoId)
                .ToListAsync();

            if (!calificaciones.Any())
                return Ok(new { promedio = 0, total = 0 });

            var promedio = calificaciones.Average(c => c.Puntuacion);
            var total = calificaciones.Count;

            return Ok(new { promedio, total });
        }

        // GET: api/calificaciones/barbero/{barberoId}/cliente/{clienteId}
        [HttpGet("barbero/{barberoId}/cliente/{clienteId}")]
        public async Task<IActionResult> GetCalificacionCliente(long barberoId, long clienteId)
        {
            var calificacion = await _context.Calificaciones
                .FirstOrDefaultAsync(c => c.BarberoId == barberoId && c.ClienteId == clienteId);

            if (calificacion == null)
                return Ok(new { puntuacion = 0 }); // 0 = sin calificación previa

            return Ok(new { puntuacion = calificacion.Puntuacion });
        }
    }
}
