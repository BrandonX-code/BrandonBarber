using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Barber.Maui.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/citas")]
[ApiController]
public class CitasController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly AppDbContext _context;

    public CitasController(AppDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    private void ConvertirCitaAFormatoLocal(Cita cita)
    {
        cita.Fecha = DateTime.SpecifyKind(cita.Fecha, DateTimeKind.Utc).ToLocalTime();
    }
    private async Task EnriquecerCitaConServicio(Cita cita)
    {
        if (cita.ServicioId.HasValue)
        {
            var servicio = await _context.Servicios.FindAsync(cita.ServicioId.Value);
            if (servicio != null)
            {
                cita.ServicioNombre = servicio.Nombre;
                cita.ServicioPrecio = servicio.Precio;
            }
        }
    }

    [HttpGet("Barberos/{idbarberia}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitas(int idbarberia)
    {
        var barberos = await _context.UsuarioPerfiles
            .Where(b => b.IdBarberia == idbarberia)
            .Select(b => new { b.Cedula, b.Nombre })
            .ToListAsync();

        var barberoDict = barberos.ToDictionary(b => b.Cedula, b => b.Nombre);
        var barberoIds = barberos.Select(b => b.Cedula).ToList();

        var citas = await _context.Citas
            .Where(c => barberoIds.Contains(c.BarberoId))
            .OrderBy(c => c.Fecha)
            .ToListAsync();

        foreach (var cita in citas)
        {
            ConvertirCitaAFormatoLocal(cita);
            cita.BarberoNombre = barberoDict.GetValueOrDefault(cita.BarberoId, "No encontrado");
            await EnriquecerCitaConServicio(cita);
        }

        return Ok(citas);
    }

    [HttpGet("todas")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetTodasLasCitas()
    {
        try
        {
            var citas = await _context.Citas
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();

            var barberoIds = citas.Select(c => c.BarberoId).Distinct().ToList();
            var barberos = await _context.UsuarioPerfiles
                .Where(b => barberoIds.Contains(b.Cedula))
                .ToDictionaryAsync(b => b.Cedula, b => b.Nombre);

            foreach (var cita in citas)
            {
                ConvertirCitaAFormatoLocal(cita);
                cita.BarberoNombre = barberos.GetValueOrDefault(cita.BarberoId, "No encontrado");
                await EnriquecerCitaConServicio(cita);
            }

            return Ok(citas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener todas las citas.", error = ex.Message });
        }
    }

    [HttpGet("barberia/{idBarberia}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitasPorBarberia(int idBarberia)
    {
        try
        {
            var barberos = await _context.UsuarioPerfiles
                .Where(b => b.IdBarberia == idBarberia)
                .Select(b => new { b.Cedula, b.Nombre })
                .ToListAsync();

            var barberoDict = barberos.ToDictionary(b => b.Cedula, b => b.Nombre);
            var barberoIds = barberos.Select(b => b.Cedula).ToList();

            var citas = await _context.Citas
                .Where(c => barberoIds.Contains(c.BarberoId))
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();

            foreach (var cita in citas)
            {
                ConvertirCitaAFormatoLocal(cita);
                cita.BarberoNombre = barberoDict.GetValueOrDefault(cita.BarberoId, "No encontrado");
                await EnriquecerCitaConServicio(cita);
            }

            return Ok(citas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener citas de la barbería.", error = ex.Message });
        }
    }

    [HttpGet("by-date-range/{fechaInicio}/{fechaFin}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitasPorRangoFechas(DateTime fechaInicio, DateTime fechaFin)
    {
        try
        {
            fechaInicio = fechaInicio.ToUniversalTime();
            fechaFin = fechaFin.ToUniversalTime();

            var citas = await _context.Citas
                .Where(c => c.Fecha >= fechaInicio && c.Fecha <= fechaFin)
                .OrderBy(c => c.Fecha)
                .ToListAsync();

            var barberoIds = citas.Select(c => c.BarberoId).Distinct().ToList();
            var barberos = await _context.UsuarioPerfiles
                .Where(b => barberoIds.Contains(b.Cedula))
                .ToDictionaryAsync(b => b.Cedula, b => b.Nombre);

            foreach (var cita in citas)
            {
                ConvertirCitaAFormatoLocal(cita);
                cita.BarberoNombre = barberos.GetValueOrDefault(cita.BarberoId, "No encontrado");
                await EnriquecerCitaConServicio(cita);
            }

            return Ok(citas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener citas por rango de fechas.", error = ex.Message });
        }
    }

    [HttpGet("by-date-range/{fechaInicio}/{fechaFin}/{idBarberia}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitasPorRangoFechasYBarberia(DateTime fechaInicio, DateTime fechaFin, int idBarberia)
    {
        try
        {
            fechaInicio = fechaInicio.ToUniversalTime();
            fechaFin = fechaFin.ToUniversalTime();

            var barberos = await _context.UsuarioPerfiles
                .Where(b => b.IdBarberia == idBarberia)
                .Select(b => new { b.Cedula, b.Nombre })
                .ToListAsync();

            var barberoDict = barberos.ToDictionary(b => b.Cedula, b => b.Nombre);
            var barberoIds = barberos.Select(b => b.Cedula).ToList();

            var citas = await _context.Citas
                .Where(c => c.Fecha >= fechaInicio && c.Fecha <= fechaFin && barberoIds.Contains(c.BarberoId))
                .OrderBy(c => c.Fecha)
                .ToListAsync();

            foreach (var cita in citas)
            {
                ConvertirCitaAFormatoLocal(cita);
                cita.BarberoNombre = barberoDict.GetValueOrDefault(cita.BarberoId, "No encontrado");
                await EnriquecerCitaConServicio(cita);
            }

            return Ok(citas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener citas por fecha y barbería.", error = ex.Message });
        }
    }

    [HttpGet("{cedula}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitasPorCedula(long cedula)
    {
        var citas = await _context.Citas
            .Where(c => c.Cedula == cedula)
            .ToListAsync();

        foreach (var cita in citas)
        {
            ConvertirCitaAFormatoLocal(cita);
            var barbero = await _context.UsuarioPerfiles
                .FirstOrDefaultAsync(b => b.Cedula == cita.BarberoId);

            cita.BarberoNombre = barbero?.Nombre ?? "No encontrado";
            await EnriquecerCitaConServicio(cita);
        }

        return Ok(citas);
    }

    [HttpGet("by-date/{fecha}&{idBarberia}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitasPorFecha(DateTime fecha, int idBarberia)
    {
        fecha = fecha.ToUniversalTime();

        var barberos = await _context.UsuarioPerfiles
            .Where(b => b.IdBarberia == idBarberia && b.Rol == "barbero")
            .Select(b => new { b.Cedula, b.Nombre })
            .ToListAsync();

        var barberoDict = barberos.ToDictionary(b => b.Cedula, b => b.Nombre);
        var barberoIds = barberos.Select(b => b.Cedula).ToList();

        var citas = await _context.Citas
            .Where(c => c.Fecha.Date == fecha.Date && barberoIds.Contains(c.BarberoId))
            .OrderBy(c => c.Fecha)
            .ToListAsync();

        foreach (var cita in citas)
        {
            ConvertirCitaAFormatoLocal(cita);
            cita.BarberoNombre = barberoDict.GetValueOrDefault(cita.BarberoId, "No encontrado");
            await EnriquecerCitaConServicio(cita);
        }

        return Ok(citas);
    }

    [HttpGet("barbero/{barberoId}/fecha/{fecha}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitasPorBarberoYFecha(long barberoId, DateTime fecha)
    {
        fecha = fecha.ToUniversalTime();

        var citas = await _context.Citas
            .Where(c => c.BarberoId == barberoId && c.Fecha.Date == fecha.Date)
            .OrderBy(c => c.Fecha)
            .ToListAsync();

        foreach (var cita in citas)
        {
            ConvertirCitaAFormatoLocal(cita);
            await EnriquecerCitaConServicio(cita);
        }

        return Ok(citas);
    }

    [HttpGet("barbero/{barberoId}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitasPorBarbero(long barberoId)
    {
        try
        {
            var citas = await _context.Citas
                .Where(c => c.BarberoId == barberoId)
                .OrderBy(c => c.Fecha)
                .ToListAsync();

            var barbero = await _context.UsuarioPerfiles
                .FirstOrDefaultAsync(b => b.Cedula == barberoId);

            foreach (var cita in citas)
            {
                ConvertirCitaAFormatoLocal(cita);
                cita.BarberoNombre = barbero?.Nombre ?? "No encontrado";
                await EnriquecerCitaConServicio(cita);
            }

            return Ok(citas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener citas del barbero.", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Cita>> CrearCita([FromBody] Cita nuevaCita)
    {
        if (nuevaCita == null || string.IsNullOrWhiteSpace(nuevaCita.Nombre))
        {
            return BadRequest("Datos inválidos.");
        }

        // 🔥 AGREGAR ESTO: Convertir la fecha a UTC antes de guardar
        //nuevaCita.Fecha = nuevaCita.Fecha.ToUniversalTime();
        nuevaCita.Fecha = DateTime.SpecifyKind(nuevaCita.Fecha, DateTimeKind.Utc);

        bool existeCita = await _context.Citas
            .AnyAsync(c => c.Fecha == nuevaCita.Fecha && c.BarberoId == nuevaCita.BarberoId);

        if (existeCita)
        {
            return Conflict("Ya existe una cita en esta fecha y hora.");
        }

        try
        {
            await EnriquecerCitaConServicio(nuevaCita);
            _context.Citas.Add(nuevaCita);
            await _context.SaveChangesAsync();

            // 🔥 MODIFICAR LA NOTIFICACIÓN: Convertir a hora local de Colombia
            var zonaColombia = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
            var fechaLocal = TimeZoneInfo.ConvertTimeFromUtc(nuevaCita.Fecha, zonaColombia);


            var data = new Dictionary<string, string>
        {
            { "tipo", "nueva_cita" },
            { "citaId", nuevaCita.Id.ToString() },
            { "clienteNombre", nuevaCita.Nombre ?? "" }
        };

            await _notificationService.EnviarNotificacionAsync(
                nuevaCita.BarberoId,
                "Nueva Cita Pendiente",
                $"{nuevaCita.Nombre} ha solicitado una cita para el {fechaLocal:dd/MM/yyyy - hh:mm tt}", // 🔥 FORMATO CORREGIDO
                data
            );

            ConvertirCitaAFormatoLocal(nuevaCita);
            return StatusCode(201, nuevaCita);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al guardar la cita.", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarCita(int id)
    {
        var cita = await _context.Citas.FindAsync(id);
        if (cita == null)
            return NotFound();

        _context.Citas.Remove(cita);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}/estado")]
    public async Task<IActionResult> ActualizarEstado(int id, [FromBody] EstadoRequest req)
    {
        var cita = await _context.Citas.FirstOrDefaultAsync(c => c.Id == id);
        if (cita == null) return NotFound();

        var barbero = await _context.UsuarioPerfiles
            .FirstOrDefaultAsync(u => u.Cedula == cita.BarberoId);

        var servicio = await _context.Servicios
            .FirstOrDefaultAsync(s => s.Id == cita.ServicioId);

        string barberoNombre = barbero?.Nombre ?? "el barbero";
        string servicioNombre = servicio?.Nombre ?? "tu servicio";

        // 🔥 Convertir fecha a hora local de Colombia
        var zonaColombia = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
        var fechaLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(cita.Fecha, DateTimeKind.Utc), zonaColombia);

        cita.Estado = req.Estado;
        await _context.SaveChangesAsync();

        // 🔥 ENVIAR SOLO UNA NOTIFICACIÓN según el estado
        if (req.Estado == "Completada")
        {
            await _notificationService.EnviarNotificacionAsync(
                cita.Cedula,
                "Cita Aceptada",
                $"Tu cita con {barberoNombre} para {servicioNombre} el {fechaLocal:dd/MM/yyyy - hh:mm tt} fue aceptada",
                new Dictionary<string, string> { { "tipo", "cita_aceptada" } }
            );
        }
        else if (req.Estado == "Cancelada")
        {
            await _notificationService.EnviarNotificacionAsync(
                cita.Cedula,
                "Cita Rechazada",
                $"Tu cita con {barberoNombre} para {servicioNombre} el {fechaLocal:dd/MM/yyyy - hh:mm tt} fue rechazada",
                new Dictionary<string, string> { { "tipo", "cita_rechazada" } }
            );
        }

        return Ok();
    }

    public class EstadoRequest { public string Estado { get; set; } = string.Empty; }

}

public class EstadoUpdateDto
{
    public string Estado { get; set; } = string.Empty;
}
