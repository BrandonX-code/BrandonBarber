using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Barber.Maui.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Barber.Maui.API.Controllers
{
    [Route("api/solicitudes")]
    [ApiController]
    public class SolicitudesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public SolicitudesController(AppDbContext context, IEmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
        }

        [HttpPost("crear")]
        public async Task<ActionResult> CrearSolicitud([FromBody] SolicitudAdmin solicitud)
        {
            if (solicitud == null)
                return BadRequest("Datos inválidos");

            // Verificar si ya existe una solicitud pendiente
            var existe = await _context.SolicitudesAdmin
                .AnyAsync(s => s.CedulaSolicitante == solicitud.CedulaSolicitante &&
                              s.Estado == "Pendiente");

            if (existe)
                return Conflict(new { Message = "Ya tienes una solicitud pendiente" });

            _context.SolicitudesAdmin.Add(solicitud);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Solicitud creada exitosamente" });
        }

        [HttpGet("pendientes")]
        public async Task<ActionResult<List<SolicitudAdmin>>> GetPendientes()
        {
            var solicitudes = await _context.SolicitudesAdmin
                .Where(s => s.Estado == "Pendiente")
                .OrderBy(s => s.FechaSolicitud)
                .ToListAsync();

            return Ok(solicitudes);
        }

        [HttpPut("{id}/aprobar")]
        public async Task<ActionResult> Aprobar(int id, [FromBody] AprobarRequest request)
        {
            var solicitud = await _context.SolicitudesAdmin.FindAsync(id);

            if (solicitud == null)
                return NotFound();

            solicitud.Estado = "Aprobado";
            solicitud.FechaRespuesta = DateTime.Now;
            solicitud.CedulaRevisor = request.CedulaRevisor;

            await _context.SaveChangesAsync();

            // Generar link CORRECTO con tu IP
            var baseUrl = $"{Request.Scheme}://{Request.Host}"; // Esto toma automáticamente la URL del servidor
            var linkRegistro = $"{baseUrl}/AdminRegister?solicitudId={solicitud.Id}";

            // Enviar correo
            await _emailService.SendSolicitudAprobadaEmailAsync(
                solicitud.EmailSolicitante!,
                solicitud.NombreSolicitante!,
                linkRegistro
            );

            return Ok(new { Message = "Solicitud aprobada y correo enviado" });
        }

        [HttpPut("{id}/rechazar")]
        public async Task<ActionResult> Rechazar(int id, [FromBody] RechazarRequest request)
        {
            var solicitud = await _context.SolicitudesAdmin.FindAsync(id);

            if (solicitud == null)
                return NotFound();

            solicitud.Estado = "Rechazado";
            solicitud.FechaRespuesta = DateTime.Now;
            solicitud.CedulaRevisor = request.CedulaRevisor;
            solicitud.MotivoRechazo = request.MotivoRechazo;

            await _context.SaveChangesAsync();

            // Enviar correo de rechazo
            await _emailService.SendSolicitudRechazadaEmailAsync(
                solicitud.EmailSolicitante!,
                solicitud.NombreSolicitante!,
                request.MotivoRechazo ?? ""
            );

            return Ok(new { Message = "Solicitud rechazada y correo enviado" });
        }
    }

    public class AprobarRequest
    {
        public long CedulaRevisor { get; set; }
    }

    public class RechazarRequest
    {
        public long CedulaRevisor { get; set; }
        public string? MotivoRechazo { get; set; }
    }
}