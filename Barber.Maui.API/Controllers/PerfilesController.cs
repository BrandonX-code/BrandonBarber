using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace Barber.Maui.API.Controllers
{
    [Route("api/perfiles")]
    [ApiController]
    public class UsuarioPerfilesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Cloudinary _cloudinary;

        public UsuarioPerfilesController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            var account = new Account(
                configuration["Cloudinary:CloudName"],
                configuration["Cloudinary:ApiKey"],
                configuration["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Auth>>> GetUsuarioPerfiles()
        {
            return await _context.UsuarioPerfiles.ToListAsync();
        }

        [HttpGet("{cedula}")]
        public async Task<ActionResult<Auth>> GetAuthPorCedula(long cedula)
        {
            var auth = await _context.UsuarioPerfiles.FirstOrDefaultAsync(p => p.Cedula == cedula);
            return auth == null ? NotFound() : Ok(auth);
        }

        [HttpPost]
        public async Task<ActionResult<Auth>> CrearAuth([FromBody] Auth nuevoAuth)
        {
            if (nuevoAuth == null || string.IsNullOrWhiteSpace(nuevoAuth.Nombre))
                return BadRequest(new { message = "Error: Datos inválidos en la solicitud." });

            if (await _context.UsuarioPerfiles.AnyAsync(p => p.Cedula == nuevoAuth.Cedula))
                return Conflict(new { message = "Ya existe un Auth con esta cédula." });

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
        public async Task<IActionResult> ActualizarAuth(long id, [FromBody] Auth authUpdate)
        {
            if (id != authUpdate.Cedula) return BadRequest();

            var auth = await _context.UsuarioPerfiles.FirstOrDefaultAsync(u => u.Cedula == id);
            if (auth == null) return NotFound();

            auth.Nombre = authUpdate.Nombre;
            auth.Email = authUpdate.Email;
            auth.Direccion = authUpdate.Direccion;
            auth.Telefono = authUpdate.Telefono;
            auth.Contraseña = authUpdate.Contraseña;
            auth.Rol = authUpdate.Rol;
            auth.ImagenPath = authUpdate.ImagenPath;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AuthExists(id))
                {
                    return NotFound();
                }
                throw;
            }

        }

        [HttpPost("{id}/imagen")]
        public async Task<IActionResult> SubirImagenAuth(long id, IFormFile imagen)
        {
            var auth = await _context.UsuarioPerfiles.FindAsync(id);
            if (auth == null) return NotFound();

            try
            {
                var resultado = await SubirImagenACloudinary(imagen, id);
                if (resultado == null)
                    return StatusCode(500, new { message = "Error al subir imagen a Cloudinary" });

                auth.ImagenPath = resultado.SecureUrl.ToString();
                _context.Entry(auth).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new { url = auth.ImagenPath });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al subir imagen", error = ex.Message });
            }
        }

        private async Task<ImageUploadResult?> SubirImagenACloudinary(IFormFile imagen, long userId)
        {
            try
            {
                using var stream = imagen.OpenReadStream();
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(imagen.FileName, stream),
                    Folder = "perfiles_usuarios",
                    PublicId = $"perfil_{userId}_{DateTime.UtcNow.Ticks}",
                    Transformation = new Transformation()
                        .Width(500).Height(500).Crop("limit").Quality("auto")
                };

                var result = await _cloudinary.UploadAsync(uploadParams);
                return result.StatusCode == System.Net.HttpStatusCode.OK ? result : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error subiendo imagen a Cloudinary: {ex.Message}");
                return null;
            }
        }

        private bool AuthExists(long id) => _context.UsuarioPerfiles.Any(e => e.Cedula == id);
    }
}