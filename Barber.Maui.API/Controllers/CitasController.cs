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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitas()
    {
        return await _context.Citas.ToListAsync();
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
            cita.BarberoNombre = barbero.Nombre;
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
            return BadRequest("Error: Datos inválidos en la solicitud." );
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
