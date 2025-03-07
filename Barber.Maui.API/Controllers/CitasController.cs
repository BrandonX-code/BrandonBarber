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


    [HttpGet("{id}")]
    public async Task<ActionResult<IEnumerable<Cita>>> GetCitasPorId(long id)
    {
        var cita = await _context.Citas.FindAsync(id);

        if (cita == null)
        {
            return NotFound();
        }

        return Ok(cita);
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
            return CreatedAtAction(nameof(GetCitasPorId), new { id = nuevaCita.Id }, nuevaCita);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al guardar la cita.", error = ex.Message });
        }
    }


}
//    using Barber.Maui.API.Data;
//using Barber.Maui.API.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//[Route("api/citas")]
//[ApiController]
//public class CitasController : ControllerBase
//{
//    private readonly AppDbContext _context;

//    public CitasController(AppDbContext context)
//    {
//        _context = context;
//    }

//    // ✅ Obtener todas las citas
//    [HttpGet]
//    public async Task<ActionResult<IEnumerable<Cita>>> GetCitas()
//    {
//        return await _context.Citas.ToListAsync();
//    }

//    // ✅ Obtener una cita por ID
//    [HttpGet("{id}")]
//    public async Task<ActionResult<Cita>> GetCitasPorId(long id)
//    {
//        var cita = await _context.Citas.FindAsync(id);
//        if (cita == null)
//        {
//            return NotFound();
//        }
//        return Ok(cita);
//    }

//    // ✅ Crear una nueva cita (con validaciones)
//    [HttpPost]
//    public async Task<ActionResult<Cita>> CrearCita(Cita nuevaCita)
//    {
//        if (nuevaCita == null || string.IsNullOrWhiteSpace(nuevaCita.Nombre) || nuevaCita.Fecha < DateTime.Now)
//        {
//            return BadRequest("Datos de la cita no válidos.");
//        }

//        _context.Citas.Add(nuevaCita);
//        await _context.SaveChangesAsync();

//        return CreatedAtAction(nameof(GetCitasPorId), new { id = nuevaCita.Id }, nuevaCita);
//    }

//    // ✅ Actualizar una cita existente
//    [HttpPut("{id}")]
//    public async Task<IActionResult> ActualizarCita(long id, Cita citaActualizada)
//    {
//        if (id != citaActualizada.Id)
//        {
//            return BadRequest("El ID de la URL no coincide con el de la cita.");
//        }

//        _context.Entry(citaActualizada).State = EntityState.Modified;

//        try
//        {
//            await _context.SaveChangesAsync();
//        }
//        catch (DbUpdateConcurrencyException)
//        {
//            if (!CitaExiste(id))
//            {
//                return NotFound();
//            }
//            else
//            {
//                throw;
//            }
//        }

//        return NoContent();
//    }

//    // ✅ Eliminar una cita por ID
//    [HttpDelete("{id}")]
//    public async Task<IActionResult> EliminarCita(long id)
//    {
//        var cita = await _context.Citas.FindAsync(id);
//        if (cita == null)
//        {
//            return NotFound();
//        }

//        _context.Citas.Remove(cita);
//        await _context.SaveChangesAsync();

//        return NoContent();
//    }

//    // Método privado para verificar si una cita existe
//    private bool CitaExiste(long id)
//    {
//        return _context.Citas.Any(e => e.Id == id);
//    }
//}
