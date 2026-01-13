using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Barber.Maui.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;

namespace Barber.Maui.API.Controllers
{
    [Route("api/disponibilidad-excepcional")]
    [ApiController]
    public class DisponibilidadExcepcionalController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public DisponibilidadExcepcionalController(
            AppDbContext context,
            INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // GET: api/disponibilidad-excepcional/barbero/{barberoId}
        [HttpGet("barbero/{barberoId}")]
        public async Task<ActionResult<IEnumerable<DisponibilidadExcepcional>>> GetExcepcionesBarbero(long barberoId)
        {
            var excepciones = await _context.Set<DisponibilidadExcepcional>()
                .Where(e => e.BarberoId == barberoId)
                .OrderBy(e => e.Fecha)
                .ToListAsync();

            return Ok(excepciones);
        }

        // GET: api/disponibilidad-excepcional/barbero/{barberoId}/fecha/{fecha}
        [HttpGet("barbero/{barberoId}/fecha/{fecha}")]
        public async Task<ActionResult<DisponibilidadExcepcional?>> GetExcepcionPorFecha(
            long barberoId,
            DateTime fecha)
        {
            var excepcion = await _context.Set<DisponibilidadExcepcional>()
                .FirstOrDefaultAsync(e =>
                    e.BarberoId == barberoId &&
                    e.Fecha.Date == fecha.Date);

            return Ok(excepcion);
        }

        // POST: api/disponibilidad-excepcional
        [HttpPost]
        public async Task<ActionResult<DisponibilidadExcepcional>> CrearExcepcion([FromBody] DisponibilidadExcepcional excepcion)
        {
            try
            {
                if (excepcion.Fecha.Date < DateTime.Today)
                {
                    return BadRequest(new { message = "No puedes crear excepciones para fechas pasadas" });
                }

                var existente = await _context.Set<DisponibilidadExcepcional>()
                    .FirstOrDefaultAsync(e =>
                        e.BarberoId == excepcion.BarberoId &&
                        e.Fecha.Date == excepcion.Fecha.Date);

                if (existente != null)
                {
                    return Conflict(new { message = "Ya existe una excepción para este día" });
                }

                var citasAfectadas = await ObtenerCitasAfectadas(
                    excepcion.BarberoId,
                    excepcion.Fecha.Date,
                    excepcion.DiaCompleto,
                    excepcion.HorariosModificados);

                excepcion.CitasAfectadas = string.Join(",", citasAfectadas.Select(c => c.Id));

                _context.Set<DisponibilidadExcepcional>().Add(excepcion);
                await _context.SaveChangesAsync();

                if (excepcion.DiaCompleto)
                {
                    foreach (var cita in citasAfectadas)
                    {
                        _context.Citas.Remove(cita);
                    }

                    await _context.SaveChangesAsync();
                }
                else if (!string.IsNullOrEmpty(excepcion.HorariosModificados))
                {
                    // ESCENARIO 2: Horarios diferentes → NO reagendar automáticamente
                    // Marcar citas para que el cliente seleccione nueva hora
                    foreach (var cita in citasAfectadas)
                    {
                        cita.Estado = "ReagendarPendiente";
                        _context.Citas.Update(cita);
                    }

                    await _context.SaveChangesAsync();
                }
                await SincronizarConDisponibilidadNormal(excepcion);
                // 🔔 Notificar a clientes afectados
                if (citasAfectadas.Any())
                {
                    await NotificarClientesAfectados(excepcion, citasAfectadas);
                }

                return CreatedAtAction(
                    nameof(GetExcepcionPorFecha),
                    new { barberoId = excepcion.BarberoId, fecha = excepcion.Fecha },
                    excepcion);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error al crear excepción",
                    error = ex.Message
                });
            }
        }


        // PUT: api/disponibilidad-excepcional/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarExcepcion(
            int id,
            [FromBody] DisponibilidadExcepcional excepcion)
        {
            if (id != excepcion.Id)
                return BadRequest();

            var existente = await _context.Set<DisponibilidadExcepcional>().FindAsync(id);
            if (existente == null)
                return NotFound();

            existente.TipoExcepcion = excepcion.TipoExcepcion;
            existente.Motivo = excepcion.Motivo;
            existente.HorariosModificados = excepcion.HorariosModificados;
            existente.DiaCompleto = excepcion.DiaCompleto;

            // Recalcular citas afectadas
            var citasAfectadas = await ObtenerCitasAfectadas(
                existente.BarberoId,
                existente.Fecha.Date,
                existente.DiaCompleto,
                existente.HorariosModificados);

            existente.CitasAfectadas = string.Join(",", citasAfectadas.Select(c => c.Id));

            await _context.SaveChangesAsync();

            // Notificar cambios
            if (citasAfectadas.Any() && !existente.ClientesNotificados)
            {
                await NotificarClientesAfectados(existente, citasAfectadas);
            }

            return NoContent();
        }

        // DELETE: api/disponibilidad-excepcional/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarExcepcion(int id)
        {
            var excepcion = await _context.Set<DisponibilidadExcepcional>().FindAsync(id);
            if (excepcion == null)
                return NotFound();

            _context.Set<DisponibilidadExcepcional>().Remove(excepcion);
            await RestaurarDisponibilidadNormal(excepcion);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/disponibilidad-excepcional/{id}/notificar
        [HttpPost("{id}/notificar")]
        public async Task<IActionResult> NotificarClientes(int id)
        {
            var excepcion = await _context.Set<DisponibilidadExcepcional>().FindAsync(id);
            if (excepcion == null)
                return NotFound();

            var citasAfectadas = await ObtenerCitasAfectadas(
                excepcion.BarberoId,
                excepcion.Fecha.Date,
                excepcion.DiaCompleto,
                excepcion.HorariosModificados);

            await NotificarClientesAfectados(excepcion, citasAfectadas);

            excepcion.ClientesNotificados = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Clientes notificados exitosamente" });
        }

        #region Métodos Privados

        private async Task<List<Cita>> ObtenerCitasAfectadas(
            long barberoId,
            DateTime fecha,
            bool diaCompleto,
            string? horariosModificados)
        {
            var zonaColombia = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");

            var todasCitas = await _context.Citas
                .Where(c => c.BarberoId == barberoId &&
                           c.Estado != "Cancelada" &&
                           c.Estado != "Finalizada")
                .ToListAsync();

            var citasDelDia = todasCitas.Where(c =>
            {
                var fechaLocal = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(c.Fecha, DateTimeKind.Utc),
                    zonaColombia);
                return fechaLocal.Date == fecha.Date;
            }).ToList();

            if (diaCompleto)
            {
                return citasDelDia;
            }

            // Si solo hay cambio de horarios, verificar cuáles están fuera
            if (!string.IsNullOrEmpty(horariosModificados))
            {
                var horariosDict = System.Text.Json.JsonSerializer
                    .Deserialize<Dictionary<string, bool>>(horariosModificados);

                return citasDelDia.Where(cita =>
                {
                    var fechaLocal = TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(cita.Fecha, DateTimeKind.Utc),
                        zonaColombia);

                    var horaCita = fechaLocal.ToString("hh:mm tt");

                    // Verificar si la hora de la cita está en los horarios modificados
                    return !horariosDict!.Any(h =>
                        h.Value && h.Key.Contains(horaCita));
                }).ToList();
            }

            return new List<Cita>();
        }

        private async Task NotificarClientesAfectados(
            DisponibilidadExcepcional excepcion,
            List<Cita> citasAfectadas)
        {
            var barbero = await _context.UsuarioPerfiles
                .FirstOrDefaultAsync(u => u.Cedula == excepcion.BarberoId);

            var zonaColombia = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
            var fechaLocal = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(excepcion.Fecha, DateTimeKind.Utc),
                zonaColombia);

            foreach (var cita in citasAfectadas)
            {
                var citaFechaLocal = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(cita.Fecha, DateTimeKind.Utc),
                    zonaColombia);

                string mensaje;
                if (excepcion.DiaCompleto)
                {
                    mensaje = $"⚠️ AVISO IMPORTANTE" +
                              $"{barbero?.Nombre ?? "El barbero"} no estará disponible el {fechaLocal:dd/MM/yyyy}." +
                              $"Tu cita programada para las {citaFechaLocal:hh:mm tt} ha sido cancelada." +
                              $"Por favor, agenda una nueva cita en otro día disponible.";
                }
                else
                {
                    mensaje = $"📅 CAMBIO DE HORARIO" +
                              $"{barbero?.Nombre ?? "El barbero"} modificó su disponibilidad el {fechaLocal:dd/MM/yyyy}." +
                              $"Tu cita de las {citaFechaLocal:hh:mm tt} puede verse afectada." +
                              $"Por favor, verifica los nuevos horarios disponibles y reagenda si es necesario.";
                }
                if (!string.IsNullOrEmpty(excepcion.Motivo))
                {
                    mensaje += $" Motivo: {excepcion.Motivo}";
                }

                await _notificationService.EnviarNotificacionAsync(
                    cita.Cedula,
                    excepcion.DiaCompleto ? "⚠️ Cita Cancelada" : "📅 Cambio de Horario", // ✅ TÍTULO MEJORADO
                    mensaje,
                    new Dictionary<string, string>
                    {
                        { "tipo", "cita_modificada_barbero" },
                        { "citaId", cita.Id.ToString() },
                        { "excepcionId", excepcion.Id.ToString() }
                    }
                );
            }
        }
        private async Task SincronizarConDisponibilidadNormal(DisponibilidadExcepcional excepcion)
        {
            try
            {
                // Buscar disponibilidad existente para ese día
                var disponibilidad = await _context.Disponibilidad
                    .FirstOrDefaultAsync(d =>
                        d.BarberoId == excepcion.BarberoId &&
                        d.Fecha.Date == excepcion.Fecha.Date);

                if (excepcion.DiaCompleto)
                {
                    // Si es día completo, eliminar la disponibilidad
                    if (disponibilidad != null)
                    {
                        _context.Disponibilidad.Remove(disponibilidad);
                        await _context.SaveChangesAsync();
                    }
                }
                else if (!string.IsNullOrEmpty(excepcion.HorariosModificados))
                {
                    // Si son horarios modificados, actualizar o crear disponibilidad
                    if (disponibilidad == null)
                    {
                        disponibilidad = new Disponibilidad
                        {
                            Fecha = excepcion.Fecha.Date,
                            BarberoId = excepcion.BarberoId,
                            Horarios = excepcion.HorariosModificados
                        };
                        _context.Disponibilidad.Add(disponibilidad);
                    }
                    else
                    {
                        disponibilidad.Horarios = excepcion.HorariosModificados;
                        _context.Disponibilidad.Update(disponibilidad);
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sincronizando disponibilidad: {ex.Message}");
                // No lanzar excepción para no romper el flujo principal
            }
        }
        private async Task RestaurarDisponibilidadNormal(DisponibilidadExcepcional excepcion)
        {
            try
            {
                // Obtener el día de la semana
                var nombreDia = excepcion.Fecha.ToString("dddd",
                    new System.Globalization.CultureInfo("es-ES"));

                // Buscar si hay disponibilidad "estándar" para ese día de la semana
                var primerDiaDelMes = new DateTime(excepcion.Fecha.Year, excepcion.Fecha.Month, 1);
                var disponibilidadReferencia = await _context.Disponibilidad
                    .Where(d => d.BarberoId == excepcion.BarberoId &&
                               d.Fecha.Month == excepcion.Fecha.Month &&
                               d.Fecha.Year == excepcion.Fecha.Year)
                    .OrderBy(d => d.Fecha)
                    .FirstOrDefaultAsync(d => d.Fecha.DayOfWeek == excepcion.Fecha.DayOfWeek);

                if (disponibilidadReferencia != null)
                {
                    // Restaurar con los horarios del mismo día de la semana
                    var disponibilidadRestaurada = new Disponibilidad
                    {
                        Fecha = excepcion.Fecha.Date,
                        BarberoId = excepcion.BarberoId,
                        Horarios = disponibilidadReferencia.Horarios
                    };

                    _context.Disponibilidad.Add(disponibilidadRestaurada);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error restaurando disponibilidad: {ex.Message}");
            }
        }
        #endregion
    }
}