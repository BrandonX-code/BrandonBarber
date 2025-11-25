using Barber.Maui.API.Data;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Barber.Maui.API.Controllers
{
    [Route("api/notificaciones")]
    [ApiController]
    public class NotificacionesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificacionesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("enviar")]
        public async Task<IActionResult> EnviarNotificacion([FromBody] NotificacionRequest request)
        {
            try
            {
                // Obtener el token FCM del barbero desde la base de datos
                var barbero = await _context.UsuarioPerfiles
                    .FirstOrDefaultAsync(u => u.Cedula == request.BarberoId);

                if (barbero == null || string.IsNullOrEmpty(barbero.FcmToken))
                    return BadRequest(new { message = "Barbero no encontrado o sin token FCM" });

                var message = new Message()
                {
                    Token = barbero.FcmToken,
                    Notification = new Notification()
                    {
                        Title = request.Titulo,
                        Body = request.Mensaje
                    },
                    Data = new Dictionary<string, string>()
                    {
                        { "citaId", request.CitaId.ToString() },
                        { "fecha", request.Fecha.ToString("o") }
                    }
                };

                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                return Ok(new { message = "Notificación enviada", response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al enviar notificación", error = ex.Message });
            }
        }
        [HttpGet("verificar-firebase")]
        public IActionResult VerificarFirebase()
        {
            try
            {
                var app = FirebaseApp.DefaultInstance;
                if (app == null)
                    return Ok(new { inicializado = false, mensaje = "Firebase no está inicializado" });

                return Ok(new
                {
                    inicializado = true,
                    projectId = app.Options.ProjectId,
                    mensaje = "Firebase está funcionando correctamente"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }


    public class NotificacionRequest
    {
        public long BarberoId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public int CitaId { get; set; }
        public DateTime Fecha { get; set; }
    }
}