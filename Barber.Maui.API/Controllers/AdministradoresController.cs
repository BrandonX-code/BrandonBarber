using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Barber.Maui.API.Controllers
{
    [Route("api/administradores")]
    [ApiController]
    public class AdministradoresController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdministradoresController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/administradores
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Auth>>> GetAdministradores()
        {
            return await _context.UsuarioPerfiles
                .Where(u => u.Rol == "administrador")
                .ToListAsync();
        }
    }
}