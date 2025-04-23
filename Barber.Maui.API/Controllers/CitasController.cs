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

        if (citas == null || citas.Count == 0)
        {
            return NotFound();
        }

        return Ok(citas);
    }


    [HttpGet("by-date/{fecha}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitasPorFecha(DateTime fecha)
    {
        var citas = await _context.Citas
            .Where(c => c.Fecha.Date == fecha.Date)
            .ToListAsync();

        return Ok(citas);
    }

    [HttpPost]
    public async Task<ActionResult<Cita>> CrearCita([FromBody] Cita nuevaCita)
    {
        if (nuevaCita == null || string.IsNullOrWhiteSpace(nuevaCita.Nombre) || nuevaCita.Fecha < DateTime.Now)
        {
            return BadRequest(new { message = "Error: Datos inválidos en la solicitud." });
        }

        bool existeCita = await _context.Citas.AnyAsync(c => c.Fecha == nuevaCita.Fecha);

        if (existeCita)
        {
            return Conflict(new { message = "Ya existe una cita en esta fecha y hora." });
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
