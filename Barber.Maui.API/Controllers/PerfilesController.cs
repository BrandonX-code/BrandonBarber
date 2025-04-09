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
    public class PerfilesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public PerfilesController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Perfil>>> GetPerfiles()
        {
            return await _context.Perfiles.ToListAsync();
        }

        [HttpGet("{cedula}")]
        public async Task<ActionResult<Perfil>> GetPerfilPorCedula(long cedula)
        {
            var perfil = await _context.Perfiles
                .FirstOrDefaultAsync(p => p.Cedula == cedula);

            if (perfil == null)
            {
                return NotFound();
            }

            return Ok(perfil);
        }

        [HttpPost]
        public async Task<ActionResult<Perfil>> CrearPerfil([FromBody] Perfil nuevoPerfil)
        {
            if (nuevoPerfil == null || string.IsNullOrWhiteSpace(nuevoPerfil.Nombre))
            {
                return BadRequest(new { message = "Error: Datos inválidos en la solicitud." });
            }

            // Verificar si ya existe un perfil con esa cédula
            bool existePerfil = await _context.Perfiles.AnyAsync(p => p.Cedula == nuevoPerfil.Cedula);
            if (existePerfil)
            {
                return Conflict(new { message = "Ya existe un perfil con esta cédula." });
            }

            try
            {
                _context.Perfiles.Add(nuevoPerfil);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetPerfilPorCedula), new { cedula = nuevoPerfil.Cedula }, nuevoPerfil);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al guardar el perfil.", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarPerfil(int id, [FromBody] Perfil perfil)
        {
            if (id != perfil.Id)
            {
                return BadRequest();
            }

            _context.Entry(perfil).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PerfilExists(id))
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
        public async Task<IActionResult> SubirImagenPerfil(int id, IFormFile image)
        {
            var perfil = await _context.Perfiles.FindAsync(id);
            if (perfil == null)
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
                perfil.ImagenPath = $"{baseUrl}/uploads/perfiles/{uniqueFileName}";
                _context.Entry(perfil).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new { url = perfil.ImagenPath });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al guardar la imagen.", error = ex.Message });
            }
        }

        private bool PerfilExists(int id)
        {
            return _context.Perfiles.Any(e => e.Id == id);
        }
    }
}