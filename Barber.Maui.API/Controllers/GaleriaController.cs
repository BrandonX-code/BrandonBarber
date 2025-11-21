using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace Barber.Maui.API.Controllers
{
    [Route("api/galeria")]
    [ApiController]
    public class GaleriaController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Cloudinary _cloudinary;

        public GaleriaController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            
            // Configurar Cloudinary
            var account = new Account(
                configuration["Cloudinary:CloudName"],
                configuration["Cloudinary:ApiKey"],
                configuration["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
        }

        [HttpGet("barbero/{idbarbero}&{idBarberia}")]
        public async Task<ActionResult<IEnumerable<ImagenGaleria>>> ObtenerImagenes(long idbarbero, int idbarberia)
        {
            var barberos = await _context.UsuarioPerfiles
            .Where(b => b.IdBarberia == idbarberia && b.Rol == "barbero")
            .Select(b => new { b.Cedula, b.Nombre })
            .ToListAsync();

            var imagenes = await _context.ImagenesGaleria
                .Where(i => i.Activo && i.BarberoId == idbarbero)
                .OrderByDescending(i => i.FechaCreacion)
                .ToListAsync();

            return Ok(imagenes);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ImagenGaleria>> ObtenerImagenPorId(int id)
        {
            try
            {
                var imagen = await _context.ImagenesGaleria
                    .FirstOrDefaultAsync(i => i.Id == id && i.Activo);

                if (imagen == null)
                {
                    return NotFound();
                }

                return Ok(imagen);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener imagen", error = ex.Message });
            }
        }

        [HttpPost("addimg")]
        public async Task<ActionResult<ImagenGaleria>> SubirImagen(IFormFile imagen, [FromForm] string? descripcion, [FromForm] long idbarbero)
        {
            if (imagen == null || imagen.Length == 0)
            {
                return BadRequest(new { message = "No se proporcionó ninguna imagen." });
            }

            // Validar tipo de archivo
            var tiposPermitidos = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/bmp", "image/webp" };
            if (!tiposPermitidos.Contains(imagen.ContentType.ToLower()))
            {
                return BadRequest(new { message = "Tipo de archivo no permitido. Solo se permiten imágenes." });
            }

            // Validar tamaño (5MB máximo)
            if (imagen.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { message = "El archivo es muy grande. Tamaño máximo: 5MB." });
            }

            try
            {
                // Subir imagen a Cloudinary
                var uploadResult = await SubirImagenACloudinary(imagen, idbarbero);

                if (uploadResult == null)
                {
                    return StatusCode(500, new { message = "Error al subir imagen a Cloudinary" });
                }

                // Crear registro en base de datos
                var nuevaImagen = new ImagenGaleria
                {
                    NombreArchivo = uploadResult.PublicId,
                    RutaArchivo = uploadResult.SecureUrl.ToString(), // URL pública de Cloudinary
                    Descripcion = string.IsNullOrEmpty(descripcion) ? null : descripcion,
                    TipoImagen = Path.GetExtension(imagen.FileName).Replace(".", ""),
                    TamanoBytes = imagen.Length,
                    FechaCreacion = DateTime.UtcNow,
                    Activo = true,
                    BarberoId = idbarbero
                };

                _context.ImagenesGaleria.Add(nuevaImagen);
                await _context.SaveChangesAsync();

                return StatusCode(201, nuevaImagen);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al guardar la imagen.", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarImagen(int id, [FromBody] ActualizarImagenRequest request)
        {
            try
            {
                var imagen = await _context.ImagenesGaleria.FindAsync(id);

                if (imagen == null || !imagen.Activo)
                {
                    return NotFound();
                }

                imagen.Descripcion = request.Descripcion;
                imagen.FechaModificacion = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al actualizar la imagen.", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarImagen(int id)
        {
            try
            {
                var imagen = await _context.ImagenesGaleria.FindAsync(id);

                if (imagen == null)
                {
                    return NotFound();
                }

                // Eliminar de Cloudinary
                await EliminarImagenDeCloudinary(imagen.NombreArchivo!);

                // Eliminar registro de base de datos
                _context.ImagenesGaleria.Remove(imagen);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al eliminar la imagen.", error = ex.Message });
            }
        }

        // NUEVO: Ya no necesitas este endpoint porque Cloudinary proporciona URLs directas
        // [HttpGet("archivo/{nombreArchivo}")] - ELIMINADO

        #region Métodos privados para Cloudinary

        private async Task<ImageUploadResult?> SubirImagenACloudinary(IFormFile imagen, long barberoId)
        {
            try
            {
                using var stream = imagen.OpenReadStream();
                
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(imagen.FileName, stream),
                    Folder = "galeria_barberos", // Carpeta en Cloudinary
                    PublicId = $"barbero_{barberoId}_corte_{DateTime.UtcNow.Ticks}",
                    Transformation = new Transformation()
                        .Width(800) // Redimensionar para optimizar
                        .Height(800)
                        .Crop("limit")
                        .Quality("auto") // Optimización automática
                };

                var result = await _cloudinary.UploadAsync(uploadParams);
                
                return result.StatusCode == System.Net.HttpStatusCode.OK ? result : null;
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error subiendo a Cloudinary: {ex.Message}");
                return null;
            }
        }

        private async Task<bool> EliminarImagenDeCloudinary(string publicId)
        {
            try
            {
                var deleteParams = new DeletionParams(publicId);
                var result = await _cloudinary.DestroyAsync(deleteParams);
                
                return result.StatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error eliminando de Cloudinary: {ex.Message}");
                return false;
            }
        }

        #endregion
    }

    public class ActualizarImagenRequest
    {
        public string? Descripcion { get; set; }
    }
}