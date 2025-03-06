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

    [HttpPost]
    public async Task<ActionResult<Cita>> CrearCita(Cita nuevaCita)
    {
        _context.Citas.Add(nuevaCita);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCitas), new { id = nuevaCita.Id }, nuevaCita);
    }
}
