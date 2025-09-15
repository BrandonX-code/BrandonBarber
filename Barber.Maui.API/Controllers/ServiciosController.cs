using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace Barber.Maui.API.Controllers
{
    [Route("api/servicios")]
    [ApiController]
    public class ServiciosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Cloudinary _cloudinary;

        public ServiciosController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            var account = new Account(
                configuration["Cloudinary:CloudName"],
                configuration["Cloudinary:ApiKey"],
                configuration["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
        }

        [HttpGet("{idbarberia}")]
        public async Task<ActionResult<IEnumerable<ServicioModel>>> GetServicios(int idbarberia)
        {
            return await _context.Servicios.Where(s => s.IdBarberia == idbarberia).ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<ServicioModel>> CrearServicio([FromForm] ServicioModel servicio, IFormFile imageFile) // ✨ CAMBIO AQUÍ
        {
            if (servicio == null)
                return BadRequest("El servicio no puede ser nulo.");

            // ✅ Usa la nueva propiedad 'ImagenFile' del modelo
            if (imageFile != null)
            {
                var resultadoSubida = await SubirImagenACloudinary(imageFile, servicio.Nombre!);
                if (resultadoSubida == null)
                    return StatusCode(500, new { message = "Error al subir la imagen del servicio a Cloudinary." });

                servicio.Imagen = resultadoSubida.SecureUrl.ToString();
            }

            try
            {
                _context.Servicios.Add(servicio);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetServicios), new { idbarberia = servicio.IdBarberia }, servicio);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al crear el servicio.", error = ex.Message });
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> EditarServicio(int id, [FromForm] ServicioModel servicio, IFormFile? imagenFile)
        {
            if (id ==0)
                return BadRequest("El ID de la ruta no coincide con el ID del servicio.");

            var servicioExistente = await _context.Servicios.FindAsync(id);
            if (servicioExistente == null)
                return NotFound("Servicio no encontrado.");

            servicioExistente.Nombre = servicio.Nombre;
            servicioExistente.Precio = servicio.Precio;

            // ✅ Usa la nueva propiedad 'ImagenFile' del modelo
            if (imagenFile != null)
            {
                var resultadoSubida = await SubirImagenACloudinary(imagenFile, servicio.Nombre!);
                if (resultadoSubida == null)
                    return StatusCode(500, new { message = "Error al actualizar la imagen del servicio en Cloudinary." });

                servicioExistente.Imagen = resultadoSubida.SecureUrl.ToString();
            }

            _context.Entry(servicioExistente).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ServicioExists(id))
                {
                    return NotFound("El servicio ya no existe.");
                }
                throw;
            }
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

        private async Task<ImageUploadResult?> SubirImagenACloudinary(IFormFile imagenFile, string nombreServicio)
        {
            try
            {
                using var stream = imagenFile.OpenReadStream();
                var publicId = $"servicios/{nombreServicio.Replace(" ", "_").ToLower()}_{DateTime.UtcNow.Ticks}";

                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(imagenFile.FileName, stream),
                    Folder = "servicios",
                    PublicId = publicId,
                    Transformation = new Transformation()
                        .Width(800).Height(600).Crop("limit").Quality("auto")
                };

                var result = await _cloudinary.UploadAsync(uploadParams);
                return result.StatusCode == System.Net.HttpStatusCode.OK ? result : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error subiendo imagen de servicio a Cloudinary: {ex.Message}");
                return null;
            }
        }

        private bool ServicioExists(int id)
        {
            return _context.Servicios.Any(e => e.Id == id);
        }
    }
}