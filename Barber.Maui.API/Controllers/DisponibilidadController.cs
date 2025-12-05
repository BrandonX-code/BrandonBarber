using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;

namespace Barber.Maui.API.Controllers
{
    [Route("api/disponibilidad")]
    [ApiController]
    public class DisponibilidadController(AppDbContext context) : ControllerBase
    {
        private readonly AppDbContext _context = context;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Disponibilidad>>> GetDisponibilidad()
        {
            return await _context.Disponibilidad.ToListAsync();
        }

        [HttpGet("by-date")]
        public async Task<ActionResult<Disponibilidad>> GetDisponibilidadPorFecha([FromQuery] DateTime fecha, [FromQuery]long barberoId)
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

        [HttpGet("barbero/{barberoId}/fecha/{fecha}")]
        public async Task<ActionResult<List<Disponibilidad>>> GetDisponibilidadPorBarberoYFecha(long barberoId, DateTime fecha)
        {
            var disponibilidad = await _context.Disponibilidad
                .Where(d => d.BarberoId == barberoId && d.Fecha.Date == fecha.Date)
                .ToListAsync();

            return Ok(disponibilidad);
        }
        [HttpGet("barbero/{barberoId}/mes/{year}/{month}")]
        public async Task<ActionResult<List<Disponibilidad>>> GetDisponibilidadPorMes(long barberoId, int year, int month)
        {
            try
            {
                var primerDia = new DateTime(year, month, 1);
                var ultimoDia = primerDia.AddMonths(1).AddDays(-1);

                var disponibilidades = await _context.Disponibilidad
                    .Where(d => d.BarberoId == barberoId &&
                           d.Fecha.Date >= primerDia &&
                           d.Fecha.Date <= ultimoDia)
                    .OrderBy(d => d.Fecha)
                    .ToListAsync();

                return Ok(disponibilidades);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener disponibilidad", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Disponibilidad>> CrearDisponibilidad([FromBody] Disponibilidad nuevaDisponibilidad)
        {
            if (nuevaDisponibilidad == null || nuevaDisponibilidad.BarberoId <= 0)
            {
                return BadRequest(new { message = "Datos inválidos" });
            }

            try
            {
                // Solo fecha
                nuevaDisponibilidad.Fecha = nuevaDisponibilidad.Fecha.Date;

                // Normalizar horarios ANTES de guardar
                nuevaDisponibilidad.Horarios = NormalizarHorarios(nuevaDisponibilidad.Horarios);

                var disponibilidadExistente = await _context.Disponibilidad
                    .FirstOrDefaultAsync(d => d.Fecha == nuevaDisponibilidad.Fecha &&
                                              d.BarberoId == nuevaDisponibilidad.BarberoId);

                if (disponibilidadExistente != null)
                {
                    disponibilidadExistente.Horarios = nuevaDisponibilidad.Horarios;
                    _context.Disponibilidad.Update(disponibilidadExistente);
                }
                else
                {
                    nuevaDisponibilidad.Id = 0;
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
        private string NormalizarHorarios(string jsonHorarios)
        {
            if (string.IsNullOrWhiteSpace(jsonHorarios))
                return jsonHorarios;

            var dic = JsonSerializer.Deserialize<Dictionary<string, bool>>(jsonHorarios);
            if (dic == null)
                return jsonHorarios;

            var normalizado = new Dictionary<string, bool>();

            foreach (var kvp in dic)
            {
                var partes = kvp.Key.Split('-');
                if (partes.Length != 2)
                    continue;

                string inicio = NormalizarHora(partes[0].Trim());
                string fin = NormalizarHora(partes[1].Trim());

                string clave = $"{inicio} - {fin}";
                normalizado[clave] = kvp.Value;
            }

            return JsonSerializer.Serialize(normalizado);
        }

        private string NormalizarHora(string horaRaw)
        {
            horaRaw = horaRaw
                .Replace("a.m.", "AM", StringComparison.OrdinalIgnoreCase)
                .Replace("p.m.", "PM", StringComparison.OrdinalIgnoreCase)
                .Replace("a. m.", "AM", StringComparison.OrdinalIgnoreCase)
                .Replace("p. m.", "PM", StringComparison.OrdinalIgnoreCase)
                .Replace("am", "AM", StringComparison.OrdinalIgnoreCase)
                .Replace("pm", "PM", StringComparison.OrdinalIgnoreCase)
                .Trim();

            if (DateTime.TryParse(horaRaw, out var dt))
                return dt.ToString("hh:mm tt", CultureInfo.InvariantCulture);

            throw new FormatException($"Formato de hora no válido: {horaRaw}");
        }

        [HttpDelete("barbero/{barberoId}/mes/{year}/{month}")]
        public async Task<IActionResult> EliminarDisponibilidadMes(long barberoId, int year, int month)
        {
            var primerDia = new DateTime(year, month,1);
            var ultimoDia = primerDia.AddMonths(1).AddDays(-1);

            var disponibilidades = await _context.Disponibilidad
                .Where(d => d.BarberoId == barberoId &&
                    d.Fecha.Date >= primerDia &&
                    d.Fecha.Date <= ultimoDia)
                .ToListAsync();

            if (!disponibilidades.Any())
                return Ok(new { message = "No había disponibilidades para eliminar." });

            _context.Disponibilidad.RemoveRange(disponibilidades);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Disponibilidades eliminadas correctamente." });
        }

        [HttpDelete("barbero/{barberoId}/semana/{fecha}")]
        public async Task<IActionResult> EliminarDisponibilidadSemana(long barberoId, DateTime fecha)
        {
            // Calcular lunes y domingo de la semana de 'fecha' (lunes =1, domingo =7)
            int dayOfWeek = (int)fecha.DayOfWeek;
            // En .NET, Sunday =0, Monday =1, ..., Saturday =6
            int daysToMonday = dayOfWeek ==0 ?6 : dayOfWeek -1;
            var lunes = fecha.Date.AddDays(-daysToMonday);
            var domingo = lunes.AddDays(6);

            var disponibilidades = await _context.Disponibilidad
                .Where(d => d.BarberoId == barberoId &&
                    d.Fecha.Date >= lunes &&
                    d.Fecha.Date <= domingo)
                .ToListAsync();

            if (!disponibilidades.Any())
                return Ok(new { message = "No había disponibilidades para eliminar en la semana." });

            _context.Disponibilidad.RemoveRange(disponibilidades);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Disponibilidades de la semana ({lunes:yyyy-MM-dd} a {domingo:yyyy-MM-dd}) eliminadas correctamente." });
        }

    }
}