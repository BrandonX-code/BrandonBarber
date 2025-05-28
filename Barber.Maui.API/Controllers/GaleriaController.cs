using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Barber.Maui.API.Controllers
{
    [Route("api/galeria")]
    [ApiController]
    public class GaleriaController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GaleriaController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("img")]
        public async Task<ActionResult<IEnumerable<ImagenGaleria>>> ObtenerImagenes()
        {
            var imagenes = await _context.ImagenesGaleria
                .Where(i => i.Activo)
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
        public async Task<ActionResult<ImagenGaleria>> SubirImagen(IFormFile imagen,[FromForm] string descripcion)
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
                // Crear directorio si no existe
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "galeria");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Generar nombre único para el archivo
                var extension = Path.GetExtension(imagen.FileName);
                var nombreArchivo = $"corte_{DateTime.UtcNow.Ticks}.jpg";
                var rutaCompleta = Path.Combine(uploadsPath, nombreArchivo);

                // Guardar archivo físicamente
                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    await imagen.CopyToAsync(stream);
                }

                var rutaRelativa = $"/uploads/galeria/{nombreArchivo}";

                // Crear registro en base de datos
                var nuevaImagen = new ImagenGaleria
                {
                    NombreArchivo = nombreArchivo,
                    RutaArchivo = rutaRelativa,
                    Descripcion = string.IsNullOrEmpty(descripcion) ? null : descripcion, // Asegurar que vacío sea null
                    TipoImagen = extension.Replace(".", ""),
                    TamanoBytes = imagen.Length,
                    FechaCreacion = DateTime.UtcNow,
                    Activo = true
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

                // CAMBIO: Eliminación física del registro en la base de datos
                _context.ImagenesGaleria.Remove(imagen);
                await _context.SaveChangesAsync();

                // Eliminar archivo físico
                var rutaFisica = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                    imagen.RutaArchivo.TrimStart('/'));

                if (System.IO.File.Exists(rutaFisica))
                {
                    System.IO.File.Delete(rutaFisica);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al eliminar la imagen.", error = ex.Message });
            }
        }

        [HttpGet("archivo/{nombreArchivo}")]
        public async Task<IActionResult> ObtenerArchivoImagen(string nombreArchivo)
        {
            try
            {
                var rutaArchivo = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                    "uploads", "galeria", nombreArchivo);

                if (!System.IO.File.Exists(rutaArchivo))
                {
                    return NotFound();
                }

                var tipoContenido = nombreArchivo.ToLower() switch
                {
                    var name when name.EndsWith(".jpg") || name.EndsWith(".jpeg") => "image/jpeg",
                    var name when name.EndsWith(".png") => "image/png",
                    var name when name.EndsWith(".gif") => "image/gif",
                    var name when name.EndsWith(".bmp") => "image/bmp",
                    var name when name.EndsWith(".webp") => "image/webp",
                    _ => "application/octet-stream"
                };

                var bytes = await System.IO.File.ReadAllBytesAsync(rutaArchivo);
                return File(bytes, tipoContenido);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener archivo", error = ex.Message });
            }
        }
    }

    // Clase para las solicitudes de actualización
    public class ActualizarImagenRequest
    {
        public string? Descripcion { get; set; }
    }
}