using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Barber.Maui.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Barber.Maui.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public AuthController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Auth>>> GetAuth()
        {
            return await _context.UsuarioPerfiles.ToListAsync();
        }

        [HttpGet("cliente/{idbarberia}")]
        public async Task<ActionResult<IEnumerable<Auth>>> GetClientePorBarberia(int idbarberia)
        {
            return await _context.UsuarioPerfiles.Where(c => idbarberia == c.IdBarberia && c.Rol!.Equals("cliente")).ToListAsync();
        }

        [HttpGet("{cedula}")]
        public async Task<ActionResult<IEnumerable<Auth>>> GetAuthPorCedula(long cedula)
        {
            var User = await _context.UsuarioPerfiles
                .Where(c => c.Cedula == cedula)
                .ToListAsync();

            if (User == null || User.Count == 0)
            {
                return NotFound();
            }

            return Ok(User);
        }

        [HttpPost("register")]
        public async Task<ActionResult<Auth>> RegistrarUsuario([FromBody] Auth nuevoUsuario)
        {
            if (nuevoUsuario == null || string.IsNullOrWhiteSpace(nuevoUsuario.Email) || string.IsNullOrWhiteSpace(nuevoUsuario.Contraseña))
            {
                return BadRequest(new { message = "Datos inválidos. El email y la contraseña son obligatorios." });
            }

            var existe = await _context.UsuarioPerfiles.AnyAsync(u => u.Email == nuevoUsuario.Email || u.Cedula == nuevoUsuario.Cedula);
            if (existe)
            {
                return Conflict(new { message = "Ya existe un usuario con este email o cédula." });
            }

            try
            {
                _context.UsuarioPerfiles.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetAuthPorCedula), new { cedula = nuevoUsuario.Cedula }, nuevoUsuario);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al registrar el usuario.", error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<Auth>> LoginUsuario([FromBody] LoginDto credenciales)
        {
            if (credenciales == null || string.IsNullOrWhiteSpace(credenciales.Email) || string.IsNullOrWhiteSpace(credenciales.Contraseña))
            {
                return BadRequest(new { message = "Email y contraseña son obligatorios." });
            }

            var usuario = await _context.UsuarioPerfiles
                .FirstOrDefaultAsync(u => u.Email == credenciales.Email && u.Contraseña == credenciales.Contraseña);

            if (usuario == null)
            {
                return Ok(new { Success = false, Message = "Credenciales inválidas." });
            }

            var response = new
            {
                IsSuccess = true,
                Message = "Login exitoso",
                User = new
                {
                    usuario.Cedula,
                    usuario.Nombre,
                    usuario.Telefono,
                    usuario.Email,
                    usuario.Rol,
                    usuario.IdBarberia
                },
                Token = "token-placeholder"
            };

            return Ok(response);
        }


        [HttpPost("forgot-password")]
        public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new ForgotPasswordResponse
                {
                    IsSuccess = false,
                    Message = "El email es obligatorio."
                });
            }

            try
            {
                // Verificar si el usuario existe
                var usuario = await _context.UsuarioPerfiles
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (usuario == null)
                {
                    return Ok(new ForgotPasswordResponse
                    {
                        IsSuccess = false,
                        Message = "Este correo no está registrado en el sistema."
                    });
                }

                // Generar token de recuperación (6 dígitos)
                var token = GenerateRecoveryToken();

                // Eliminar tokens anteriores para este email
                var existingTokens = await _context.PasswordResets
                    .Where(pr => pr.Email == request.Email)
                    .ToListAsync();

                _context.PasswordResets.RemoveRange(existingTokens);

                // Crear nuevo token
                var passwordReset = new PasswordReset
                {
                    Email = request.Email,
                    Token = token,
                    ExpiryDate = DateTime.Now.AddMinutes(30), // 30 minutos de validez
                    IsUsed = false
                };

                _context.PasswordResets.Add(passwordReset);
                await _context.SaveChangesAsync();

                // Enviar email
                var emailSent = await _emailService.SendPasswordResetEmailAsync(
                    request.Email,
                    usuario.Nombre!,
                    token
                );

                if (!emailSent)
                {
                    return StatusCode(500, new ForgotPasswordResponse
                    {
                        IsSuccess = false,
                        Message = "Error al enviar el email. Intenta más tarde."
                    });
                }

                return Ok(new ForgotPasswordResponse
                {
                    IsSuccess = true,
                    Message = "Se ha enviado un código de recuperación a tu email."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new ForgotPasswordResponse
                {
                    IsSuccess = false,
                    Message = "Error interno del servidor."
                });
            }
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<ForgotPasswordResponse>> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Token) ||
                string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(new ForgotPasswordResponse
                {
                    IsSuccess = false,
                    Message = "Todos los campos son obligatorios."
                });
            }

            if (request.NewPassword.Length < 6)
            {
                return BadRequest(new ForgotPasswordResponse
                {
                    IsSuccess = false,
                    Message = "La nueva contraseña debe tener al menos 6 caracteres."
                });
            }

            try
            {
                // Verificar el token
                var passwordReset = await _context.PasswordResets
                    .FirstOrDefaultAsync(pr => pr.Email == request.Email &&
                                              pr.Token == request.Token &&
                                              !pr.IsUsed &&
                                              pr.ExpiryDate > DateTime.Now);

                if (passwordReset == null)
                {
                    return BadRequest(new ForgotPasswordResponse
                    {
                        IsSuccess = false,
                        Message = "Código inválido o expirado."
                    });
                }

                // Verificar que el usuario existe
                var usuario = await _context.UsuarioPerfiles
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (usuario == null)
                {
                    return NotFound(new ForgotPasswordResponse
                    {
                        IsSuccess = false,
                        Message = "Usuario no encontrado."
                    });
                }

                // Actualizar la contraseña
                usuario.Contraseña = request.NewPassword; // En producción, deberías hashear la contraseña

                // Marcar el token como usado
                passwordReset.IsUsed = true;

                await _context.SaveChangesAsync();

                return Ok(new ForgotPasswordResponse
                {
                    IsSuccess = true,
                    Message = "Contraseña actualizada exitosamente."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new ForgotPasswordResponse
                {
                    IsSuccess = false,
                    Message = "Error interno del servidor."
                });
            }
        }
        [HttpPut("cambiar-barberia/{cedula}")]
        public async Task<ActionResult> CambiarBarberia(long cedula, [FromBody] CambiarBarberiaDto dto)
        {
            var usuario = await _context.UsuarioPerfiles.FirstOrDefaultAsync(u => u.Cedula == cedula);

            if (usuario == null)
                return NotFound(new { message = "Usuario no encontrado." });

            if (usuario.Rol?.ToLower() != "cliente")
                return BadRequest(new { message = "Solo los clientes pueden cambiar de barbería." });

            // Validar que la barbería existe
            var barberia = await _context.Barberias.FindAsync(dto.IdBarberia);
            if (barberia == null)
                return NotFound(new { message = "Barbería no encontrada." });

            usuario.IdBarberia = dto.IdBarberia;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Barbería cambiada exitosamente.", idBarberia = dto.IdBarberia });
        }

        public class CambiarBarberiaDto
        {
            public int IdBarberia { get; set; }
        }
        // Tus otros endpoints existentes...
        [HttpDelete("{cedula}")]
        public async Task<IActionResult> EliminarUsuario(long cedula)
        {
            var usuario = await _context.UsuarioPerfiles.FirstOrDefaultAsync(u => u.Cedula == cedula);

            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

            _context.UsuarioPerfiles.Remove(usuario);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Usuario eliminado correctamente." });
        }

        [HttpGet("barberos/{idBarberia}")]
        public async Task<ActionResult<List<Auth>>> GetBarberos(int idbarberia = 0)
        {
            var Barberos = await _context.UsuarioPerfiles
                .Where(b => b.IdBarberia == idbarberia && b.Rol == "barbero")
                .ToListAsync();
            return Ok(Barberos);
        }

        [HttpGet("usuario/{cedula}")]
        public async Task<ActionResult<Auth>> GetUsuario(long cedula)
        {
            var usuario = await _context.UsuarioPerfiles
                .FirstOrDefaultAsync(u => u.Cedula == cedula);

            if (usuario == null)
                return NotFound();

            return Ok(usuario);
        }
        [HttpPut("guardar-token/{cedula}")]
        public async Task<IActionResult> GuardarTokenFCM(long cedula, [FromBody] TokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FcmToken))
                return BadRequest(new { message = "El token no puede estar vacío." });

            // Validación básica de formato FCM
            if (request.FcmToken.Length < 100)
                return BadRequest(new { message = "Token FCM inválido (demasiado corto)." });

            if (request.FcmToken.Contains(" "))
                return BadRequest(new { message = "Token FCM inválido (contiene espacios)." });

            var usuario = await _context.UsuarioPerfiles.FirstOrDefaultAsync(u => u.Cedula == cedula);

            if (usuario == null)
                return NotFound(new { message = "Usuario no encontrado" });

            usuario.FcmToken = request.FcmToken;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Token FCM guardado correctamente" });
        }


        public class TokenRequest
        {
            public string FcmToken { get; set; } = string.Empty;
        }
        [HttpPost("register-admin")]
        public async Task<IActionResult> RegistrarAdministrador([FromBody] RegistroAdminDto dto)
        {
            if (dto == null || dto.SolicitudId <= 0 || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Contraseña))
                return BadRequest(new { message = "Datos inválidos." });

            var solicitud = await _context.SolicitudesAdmin.FindAsync(dto.SolicitudId);
            if (solicitud == null || solicitud.Estado != "Aprobado")
                return BadRequest(new { message = "La solicitud no existe o no está aprobada." });

            // Verifica si ya existe un usuario con esa cédula o email
            var existe = await _context.UsuarioPerfiles.AnyAsync(u => u.Cedula == solicitud.CedulaSolicitante || u.Email == dto.Email);
            if (existe)
                return Conflict(new { message = "Ya existe un usuario con este email o cédula." });

            var nuevoAdmin = new Auth
            {
                Cedula = solicitud.CedulaSolicitante,
                Nombre = dto.Nombre,
                Email = dto.Email,
                Telefono = dto.Telefono,
                Direccion = dto.Direccion,
                Contraseña = dto.Contraseña,
                Rol = "administrador"
            };
            _context.UsuarioPerfiles.Add(nuevoAdmin);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Registro de administrador exitoso." });
        }

        public class RegistroAdminDto
        {
            public int SolicitudId { get; set; }
            public string? Nombre { get; set; }
            public string? Email { get; set; }
            public string? Telefono { get; set; }
            public string? Direccion { get; set; }
            public string? Contraseña { get; set; }
        }

        // Método auxiliar para generar token
        private static string GenerateRecoveryToken()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}