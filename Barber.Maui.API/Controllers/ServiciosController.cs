using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/servicios")]
[ApiController]
public class ServiciosController : ControllerBase
{
    private readonly AppDbContext _context;

    public ServiciosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServicioModel>>> GetServicios()
    {
        return await _context.Servicios.ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<ServicioModel>> CrearServicio([FromBody] ServicioModel servicio)
    {
        _context.Servicios.Add(servicio);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetServicios), new { id = servicio.Id }, servicio);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> EditarServicio(int id, [FromBody] ServicioModel servicio)
    {
        if (id != servicio.Id)
            return BadRequest();

        _context.Entry(servicio).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarServicio(int id)
    {
        var servicio = await _context.Servicios.FindAsync(id);
        if (servicio == null)
            return NotFound();

        _context.Servicios.Remove(servicio);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}