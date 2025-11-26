using Microsoft.AspNetCore.Mvc;
using Barber.Maui.API.Services;

namespace Barber.Maui.API.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost("register-token")]
        public async Task<IActionResult> RegistrarToken([FromBody] TokenRequest request)
        {

            Console.WriteLine($"📤 Enviando registro token: Cedula={request.UsuarioCedula}, Token='{request.FcmToken}'");

            Console.WriteLine($"📩 Recibida solicitud de registro de token");
            Console.WriteLine($"📩 Usuario Cedula: {request.UsuarioCedula}");
            Console.WriteLine($"📩 Token: {request.FcmToken}");

            if (string.IsNullOrEmpty(request.FcmToken) || request.UsuarioCedula <= 0)
            {
                Console.WriteLine("❌ Datos inválidos");
                return BadRequest(new { message = "Datos inválidos" });
            }

            var resultado = await _notificationService.RegistrarTokenAsync(request.UsuarioCedula, request.FcmToken);

            if (resultado)
            {
                Console.WriteLine("✅ Token registrado exitosamente");
                return Ok(new { message = "Token registrado exitosamente" });
            }

            Console.WriteLine("❌ Error al registrar token");
            return StatusCode(500, new { message = "Error al registrar token" });
        }
        public class TokenRequest
        {
            public long UsuarioCedula { get; set; }
            public string FcmToken { get; set; } = string.Empty;
        }
    }
}