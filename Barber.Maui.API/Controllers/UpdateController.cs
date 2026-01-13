using Microsoft.AspNetCore.Mvc;

namespace Barber.Maui.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UpdateController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public UpdateController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET: api/update/check
        [HttpGet("check")]
        public IActionResult CheckUpdate()
        {
            Console.WriteLine("Verificando actualización de la aplicación...");
            try
            {
                var currentVersion = _configuration["AppUpdate:CurrentVersion"] ?? "1.0.18";
                var apkUrl = _configuration["AppUpdate:ApkUrl"] ?? "";
                var mensaje = _configuration["AppUpdate:UpdateMessage"] ?? "Nueva versión disponible";

                var updateInfo = new
                {
                    version = currentVersion,
                    apkUrl = apkUrl,
                    mensaje = mensaje
                };

                return Ok(updateInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al verificar actualización", error = ex.Message });
            }
        }
    }
}