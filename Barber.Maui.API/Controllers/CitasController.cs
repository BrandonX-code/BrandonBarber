using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/citas")]
[ApiController]
public class CitasController : ControllerBase
{
    private readonly AppDbContext _context;

    public CitasController(AppDbContext context)
    {
        _context = context;
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
        if (nuevaCita == null ||
            string.IsNullOrWhiteSpace(nuevaCita.Nombre))
        {
            return BadRequest("Datos inválidos.");
        }

        // Convertir fecha local → UTC antes de guardar
        nuevaCita.Fecha = nuevaCita.Fecha.ToUniversalTime();

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

            // Convertimos a local antes de enviarla
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
    public async Task<IActionResult> ActualizarEstado(int id, [FromBody] EstadoUpdateDto dto)
    {
        var cita = await _context.Citas.FindAsync(id);
        if (cita == null)
            return NotFound();

        cita.Estado = dto.Estado;
        await _context.SaveChangesAsync();

        return Ok();
    }
}

public class EstadoUpdateDto
{
    public string Estado { get; set; } = string.Empty;
}
