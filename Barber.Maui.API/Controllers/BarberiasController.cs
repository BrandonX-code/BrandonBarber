using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Barber.Maui.API.Controllers
{
    [ApiController]
    [Route("api/barberias")]
    public class BarberiasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Cloudinary _cloudinary;
        private readonly IConfiguration _configuration;

        public BarberiasController(
            AppDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

            // Configuración de Cloudinary
            var cloudName = _configuration["Cloudinary:CloudName"];
            var apiKey = _configuration["Cloudinary:ApiKey"];
            var apiSecret = _configuration["Cloudinary:ApiSecret"];

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }

        // GET: api/barberias
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Barberia>>> GetBarberias()
        {
            return await _context.Barberias
                .Select(b => BarberiaToDto(b))
                .ToListAsync();
        }

        // GET: api/barberias/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Barberia>> GetBarberia(int id)
        {
            var barberia = await _context.Barberias.FindAsync(id);

            if (barberia == null)
            {
                return NotFound();
            }

            return BarberiaToDto(barberia);
        }

        // POST: api/barberias
        [HttpPost]
        public async Task<ActionResult<Barberia>> PostBarberia(Barberia barberiaDto)
        {
            var barberia = new Barberia
            {
                Idadministrador = barberiaDto.Idadministrador,
                Nombre = barberiaDto.Nombre,
                Telefono = barberiaDto.Telefono,
                Direccion = barberiaDto.Direccion,
                Email = barberiaDto.Email,
                LogoUrl = barberiaDto.LogoUrl
            };

            _context.Barberias.Add(barberia);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBarberia),
                new { id = barberia.Idbarberia },
                BarberiaToDto(barberia));
        }

        // PUT: api/barberias/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBarberia(int id, Barberia barberiaDto)
        {
            if (id != barberiaDto.Idbarberia)
            {
                return BadRequest();
            }

            var barberia = await _context.Barberias.FindAsync(id);
            if (barberia == null)
            {
                return NotFound();
            }

            barberia.Nombre = barberiaDto.Nombre;
            barberia.Telefono = barberiaDto.Telefono;
            barberia.Direccion = barberiaDto.Direccion;
            barberia.Email = barberiaDto.Email;
            barberia.LogoUrl = barberiaDto.LogoUrl;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BarberiaExists(id))
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

        // DELETE: api/barberias/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBarberia(int id)
        {
            var barberia = await _context.Barberias.FindAsync(id);
            if (barberia == null)
            {
                return NotFound();
            }

            // Eliminar imagen de Cloudinary si existe
            if (!string.IsNullOrEmpty(barberia.LogoUrl))
            {
                await EliminarLogoCloudinary(barberia.LogoUrl);
            }

            _context.Barberias.Remove(barberia);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/barberias/5/logo
        [HttpPost("{id}/logo")]
        public async Task<ActionResult> UploadLogo(int id, IFormFile file)
        {
            var barberia = await _context.Barberias.FindAsync(id);
            if (barberia == null)
            {
                return NotFound("Barbería no encontrada");
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest("Archivo no válido");
            }

            // Validar tamaño máximo
            var maxFileSize = _configuration.GetValue<long>("FileSettings:MaxLogoSize", 5 * 1024 * 1024);
            if (file.Length > maxFileSize)
            {
                return BadRequest($"Tamaño máximo excedido: {maxFileSize / (1024 * 1024)} MB");
            }

            // Validar extensión
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
            {
                return BadRequest("Formato de archivo no permitido");
            }

            try
            {
                // Eliminar logo anterior si existe
                if (!string.IsNullOrEmpty(barberia.LogoUrl))
                {
                    await EliminarLogoCloudinary(barberia.LogoUrl);
                }

                // Subir nuevo logo a Cloudinary
                var uploadResult = await SubirLogoACloudinary(file, id);

                if (uploadResult.Error != null)
                {
                    return StatusCode((int)HttpStatusCode.InternalServerError,
                        new { message = "Error al subir imagen", error = uploadResult.Error.Message });
                }

                // Actualizar la barbería con la nueva URL
                barberia.LogoUrl = uploadResult.SecureUrl.ToString();
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Url = barberia.LogoUrl,
                    PublicId = uploadResult.PublicId
                });
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "Error al procesar la imagen", error = ex.Message });
            }
        }

        private async Task<ImageUploadResult> SubirLogoACloudinary(IFormFile file, int barberiaId)
        {
            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "barberias/logos",
                PublicId = $"barberia_{barberiaId}",
                Transformation = new Transformation()
                    .Width(500).Height(500).Crop("fill").Quality("auto"),
                Overwrite = true
            };

            return await _cloudinary.UploadAsync(uploadParams);
        }

        private async Task EliminarLogoCloudinary(string imageUrl)
        {
            try
            {
                // Extraer public_id de la URL
                var uri = new Uri(imageUrl);
                var publicId = Path.GetFileNameWithoutExtension(uri.AbsolutePath)
                    .Replace("barberias/logos/", "");

                var deleteParams = new DeletionParams(publicId);
                await _cloudinary.DestroyAsync(deleteParams);
            }
            catch
            {
                // Ignorar errores al eliminar
            }
        }

        private bool BarberiaExists(int id)
        {
            return _context.Barberias.Any(e => e.Idbarberia == id);
        }

        private static Barberia BarberiaToDto(Barberia barberia) =>
            new Barberia
            {
                Idbarberia = barberia.Idbarberia,
                Idadministrador = barberia.Idadministrador,
                Nombre = barberia.Nombre,
                Telefono = barberia.Telefono,
                Direccion = barberia.Direccion,
                Email = barberia.Email,
                LogoUrl = barberia.LogoUrl
            };
    }
}