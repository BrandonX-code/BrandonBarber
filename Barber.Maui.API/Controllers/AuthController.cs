using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Barber.Maui.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Auth>>> GetAuth()
        {
            return await _context.UsuarioPerfiles.ToListAsync();
        }
        [HttpGet("{cedula}")]
        public async Task<ActionResult<IEnumerable<Auth>>> GetAuthPorCedula(long cedula)
        {
            var User = await _context.UsuarioPerfiles
                .Where(c => c.Cedula == cedula)
                .ToListAsync();

            if (User == null || User.Count == 0)
            {
                return NotFound();
            }

            return Ok(User);
        }
        [HttpPost("register")]
        public async Task<ActionResult<Auth>> RegistrarUsuario([FromBody] Auth nuevoUsuario)

        {
            if (nuevoUsuario == null || string.IsNullOrWhiteSpace(nuevoUsuario.Email) || string.IsNullOrWhiteSpace(nuevoUsuario.Contraseña))
            {
                return BadRequest(new { message = "Datos inválidos. El email y la contraseña son obligatorios." });
            }

            // Verificar si ya existe un usuario con el mismo email o cédula
            var existe = await _context.UsuarioPerfiles.AnyAsync(u => u.Email == nuevoUsuario.Email || u.Cedula == nuevoUsuario.Cedula);
            if (existe)
            {
                return Conflict(new { message = "Ya existe un usuario con este email o cédula." });
            }

            try
            {
                // NO asignar valor explícito al Id
                _context.UsuarioPerfiles.Add(nuevoUsuario); // El Id debe ser asignado por la base de datos
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetAuthPorCedula), new { cedula = nuevoUsuario.Cedula }, nuevoUsuario);
            }
            catch (Exception ex)
            {
                // Deberías verificar el detalle de la "inner exception" aquí
                return StatusCode(500, new { message = "Error al registrar el usuario.", error = ex.Message });
            }
        }
        [HttpPost("login")]
        public async Task<ActionResult<Auth>> LoginUsuario([FromBody] LoginDto credenciales)
        {
            if (credenciales == null || string.IsNullOrWhiteSpace(credenciales.Email) || string.IsNullOrWhiteSpace(credenciales.Contraseña))
            {
                return BadRequest(new { message = "Email y contraseña son obligatorios." });
            }

            var usuario = await _context.UsuarioPerfiles
                .FirstOrDefaultAsync(u => u.Email == credenciales.Email && u.Contraseña == credenciales.Contraseña);

            if (usuario == null)
            {
                return Ok(new { Success = false, Message = "Credenciales inválidas." });

            }

            // Crear un objeto de respuesta para evitar enviar la contraseña
            var response = new
            {
                IsSuccess = true,
                Message = "Login exitoso",
                User = new
                {
                    usuario.Cedula,
                    usuario.Nombre,
                    usuario.Email,
                    usuario.Rol,
                    usuario.IdBarberia
                    // No incluir la contraseña
                },
                Token = "token-placeholder" // Aquí deberías generar un token JWT real
            };

            return Ok(response);
        }

        [HttpDelete("{cedula}")]
        public async Task<IActionResult> EliminarUsuario(long cedula)
        {
            var usuario = await _context.UsuarioPerfiles.FirstOrDefaultAsync(u => u.Cedula == cedula);

            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

            _context.UsuarioPerfiles.Remove(usuario);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Usuario eliminado correctamente." });
        }
        [HttpGet("barberos")]
        public async Task<ActionResult<List<Auth>>> GetBarberos()
        {
            var barberos = await _context.UsuarioPerfiles
                .Where(u => u.Rol.ToLower() == "barbero")
                .ToListAsync();
            return Ok(barberos);
        }

        [HttpGet("usuario/{cedula}")]
        public async Task<ActionResult<Auth>> GetUsuario(long cedula)
        {
            var usuario = await _context.UsuarioPerfiles
                .FirstOrDefaultAsync(u => u.Cedula == cedula);

            if (usuario == null)
                return NotFound();

            return Ok(usuario);
        }
    }
}
