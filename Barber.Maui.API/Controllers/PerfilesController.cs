using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Barber.Maui.API.Controllers
{
    [Route("api/perfiles")]
    [ApiController]
    public class UsuarioPerfilesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public UsuarioPerfilesController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Auth>>> GetUsuarioPerfiles()
        {
            return await _context.UsuarioPerfiles.ToListAsync();
        }

        [HttpGet("{cedula}")]
        public async Task<ActionResult<Auth>> GetAuthPorCedula(long cedula)
        {
            var Auth = await _context.UsuarioPerfiles
                .FirstOrDefaultAsync(p => p.Cedula == cedula);

            if (Auth == null)
            {
                return NotFound();
            }

            return Ok(Auth);
        }

        [HttpPost]
        public async Task<ActionResult<Auth>> CrearAuth([FromBody] Auth nuevoAuth)
        {
            if (nuevoAuth == null || string.IsNullOrWhiteSpace(nuevoAuth.Nombre))
            {
                return BadRequest(new { message = "Error: Datos inválidos en la solicitud." });
            }

            // Verificar si ya existe un Auth con esa cédula
            bool existeAuth = await _context.UsuarioPerfiles.AnyAsync(p => p.Cedula == nuevoAuth.Cedula);
            if (existeAuth)
            {
                return Conflict(new { message = "Ya existe un Auth con esta cédula." });
            }

            try
            {
                _context.UsuarioPerfiles.Add(nuevoAuth);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetAuthPorCedula), new { cedula = nuevoAuth.Cedula }, nuevoAuth);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al guardar el Auth.", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarAuth(long id, [FromBody] Auth Auth)
        {
            if (id != Auth.Cedula)
            {
                return BadRequest();
            }
            var auth = await _context.UsuarioPerfiles.FirstOrDefaultAsync(u => u.Cedula == id);

            //_context.Entry(Auth).State = EntityState.Modified;
            auth.Nombre = Auth.Nombre;
            auth.Email = Auth.Email;
            auth.Direccion = Auth.Direccion;
            auth.Telefono = Auth.Telefono;
            auth.Contraseña = Auth.Contraseña;
            auth.Rol = Auth.Rol;
            auth.ImagenPath = Auth.ImagenPath;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AuthExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost("{id}/imagen")]
        public async Task<IActionResult> SubirImagenAuth(long id, IFormFile image)
        {
            var Auth = await _context.UsuarioPerfiles.FindAsync(id);
            if (Auth == null)
            {
                return NotFound();
            }

            try
            {
                // Crear directorio para imágenes si no existe
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "perfiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generar nombre único para la imagen
                var uniqueFileName = $"{id}_{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Guardar la imagen en el servidor
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                // Actualizar la ruta de la imagen en la base de datos
                var baseUrl = $"{Request.Scheme}://{Request.Host.Value}";
                Auth.ImagenPath = $"{baseUrl}/uploads/perfiles/{uniqueFileName}";
                _context.Entry(Auth).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new { url = Auth.ImagenPath });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al guardar la imagen.", error = ex.Message });
            }
        }

        private bool AuthExists(long id)
        {
            return _context.UsuarioPerfiles.Any(e => e.Cedula == id);
        }
    }
}