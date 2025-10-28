using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Barber.Maui.API.Controllers
{
    [Route("api/solicitudes")]
    [ApiController]
    public class SolicitudesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SolicitudesController(AppDbContext context)
        {
            _context = context;
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

            // Crear usuario administrador
            var nuevoAdmin = new Auth
            {
                Cedula = solicitud.CedulaSolicitante,
                Nombre = solicitud.NombreSolicitante,
                Email = solicitud.EmailSolicitante,
                Telefono = solicitud.TelefonoSolicitante,
                Contraseña = "Admin123", // Contraseña temporal
                Rol = "administrador"
            };

            _context.UsuarioPerfiles.Add(nuevoAdmin);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Solicitud aprobada y usuario creado" });
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

            return Ok(new { Message = "Solicitud rechazada" });
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