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

    [HttpGet("Barberos/{idbarberia}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitas(int idbarberia)
    {
        var barberos = await _context.UsuarioPerfiles
            .Where(b => b.IdBarberia == idbarberia)
            .Select(b => new { b.Cedula, b.Nombre })
            .ToListAsync();
        var barberoIds = barberos.Select(b => b.Cedula).ToList();
        var citas = await _context.Citas
            .Where(c => barberoIds.Contains(c.BarberoId))
            .OrderBy(c => c.Fecha)
            .ToListAsync();
        return Ok(citas);
    }

    // NUEVO: Obtener todas las citas históricas del sistema
    [HttpGet("todas")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetTodasLasCitas()
    {
        try
        {
            var citas = await _context.Citas
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();

            // Llenar información de barberos
            var barberoIds = citas.Select(c => c.BarberoId).Distinct().ToList();
            var barberos = await _context.UsuarioPerfiles
                .Where(b => barberoIds.Contains(b.Cedula))
                .ToDictionaryAsync(b => b.Cedula, b => b.Nombre);

            foreach (var cita in citas)
            {
                cita.BarberoNombre = barberos.GetValueOrDefault(cita.BarberoId, "No encontrado");
            }

            return Ok(citas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener todas las citas.", error = ex.Message });
        }
    }

    // NUEVO: Obtener todas las citas históricas de una barbería específica
    [HttpGet("barberia/{idBarberia}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitasPorBarberia(int idBarberia)
    {
        try
        {
            // Obtener barberos de la barbería
            var barberos = await _context.UsuarioPerfiles
                .Where(b => b.IdBarberia == idBarberia)
                .Select(b => new { b.Cedula, b.Nombre })
                .ToListAsync();

            var barberoIds = barberos.Select(b => b.Cedula).ToList();
            var barberoDict = barberos.ToDictionary(b => b.Cedula, b => b.Nombre);

            // Obtener todas las citas de esos barberos (sin filtro de fecha)
            var citas = await _context.Citas
                .Where(c => barberoIds.Contains(c.BarberoId))
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();

            // Llenar información de barberos
            foreach (var cita in citas)
            {
                cita.BarberoNombre = barberoDict.GetValueOrDefault(cita.BarberoId, "No encontrado");
            }

            return Ok(citas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener citas de la barbería.", error = ex.Message });
        }
    }

    // NUEVO: Obtener citas por rango de fechas
    [HttpGet("by-date-range/{fechaInicio}/{fechaFin}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitasPorRangoFechas(DateTime fechaInicio, DateTime fechaFin)
    {
        try
        {
            var citas = await _context.Citas
                .Where(c => c.Fecha.Date >= fechaInicio.Date && c.Fecha.Date <= fechaFin.Date)
                .OrderBy(c => c.Fecha)
                .ToListAsync();

            // Llenar información de barberos
            var barberoIds = citas.Select(c => c.BarberoId).Distinct().ToList();
            var barberos = await _context.UsuarioPerfiles
                .Where(b => barberoIds.Contains(b.Cedula))
                .ToDictionaryAsync(b => b.Cedula, b => b.Nombre);

            foreach (var cita in citas)
            {
                cita.BarberoNombre = barberos.GetValueOrDefault(cita.BarberoId, "No encontrado");
            }

            return Ok(citas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener citas por rango de fechas.", error = ex.Message });
        }
    }

    // NUEVO: Obtener citas por rango de fechas y barbería
    [HttpGet("by-date-range/{fechaInicio}/{fechaFin}/{idBarberia}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitasPorRangoFechasYBarberia(DateTime fechaInicio, DateTime fechaFin, int idBarberia)
    {
        try
        {
            // Obtener barberos de la barbería
            var barberos = await _context.UsuarioPerfiles
                .Where(b => b.IdBarberia == idBarberia)
                .Select(b => new { b.Cedula, b.Nombre })
                .ToListAsync();

            var barberoIds = barberos.Select(b => b.Cedula).ToList();
            var barberoDict = barberos.ToDictionary(b => b.Cedula, b => b.Nombre);

            // Obtener citas en el rango de fechas para esos barberos
            var citas = await _context.Citas
                .Where(c => c.Fecha.Date >= fechaInicio.Date &&
                           c.Fecha.Date <= fechaFin.Date &&
                           barberoIds.Contains(c.BarberoId))
                .OrderBy(c => c.Fecha)
                .ToListAsync();

            // Llenar información de barberos
            foreach (var cita in citas)
            {
                cita.BarberoNombre = barberoDict.GetValueOrDefault(cita.BarberoId, "No encontrado");
            }

            return Ok(citas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener citas por rango de fechas y barbería.", error = ex.Message });
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
            Auth barbero = _context.UsuarioPerfiles.Where(b => b.Cedula == cita.BarberoId).FirstOrDefault();
            cita.BarberoNombre = barbero?.Nombre ?? "No encontrado";
        }

        return Ok(citas);
    }

    [HttpGet("by-date/{fecha}&{idBarberia}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitasPorFecha(DateTime fecha, int idbarberia)
    {
        // Obtener barberos con cédula y nombre
        var barberos = await _context.UsuarioPerfiles
            .Where(b => b.IdBarberia == idbarberia && b.Rol == "barbero")
            .Select(b => new { b.Cedula, b.Nombre })
            .ToListAsync();

        var barberoIds = barberos.Select(b => b.Cedula).ToList();
        var barberoDict = barberos.ToDictionary(b => b.Cedula, b => b.Nombre);

        var citas = await _context.Citas
            .Where(c => c.Fecha.Date == fecha.Date && barberoIds.Contains(c.BarberoId))
            .OrderBy(c => c.Fecha)
            .ToListAsync();

        // Llenar la propiedad NombreBarbero
        foreach (var cita in citas)
        {
            cita.BarberoNombre = barberoDict.GetValueOrDefault(cita.BarberoId, "No encontrado");
        }

        return Ok(citas);
    }

    [HttpGet("barbero/{barberoId}/fecha/{fecha}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitasPorBarberoYFecha(long barberoId, DateTime fecha)
    {
        var citas = await _context.Citas
            .Where(c => c.BarberoId == barberoId && c.Fecha.Date == fecha.Date)
            .OrderBy(c => c.Fecha)
            .ToListAsync();

        return Ok(citas);
    }

    [HttpPost]
    public async Task<ActionResult<Cita>> CrearCita([FromBody] Cita nuevaCita)
    {
        if (nuevaCita == null || string.IsNullOrWhiteSpace(nuevaCita.Nombre) || nuevaCita.Fecha < DateTime.Now)
        {
            return BadRequest("Error: Datos inválidos en la solicitud.");
        }

        bool existeCita = await _context.Citas.AnyAsync(c => c.Fecha == nuevaCita.Fecha && c.BarberoId == nuevaCita.BarberoId);

        if (existeCita)
        {
            return Conflict("Ya existe una cita en esta fecha y hora. Elija otro horario.");
        }

        try
        {
            _context.Citas.Add(nuevaCita);
            await _context.SaveChangesAsync();
            return StatusCode(201);
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
        {
            return NotFound();
        }

        _context.Citas.Remove(cita);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}