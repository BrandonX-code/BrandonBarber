using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Barber.Maui.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

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

        // Tus endpoints existentes...
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

        // 🔥 NUEVOS ENDPOINTS PARA RECUPERACIÓN DE CONTRASEÑA 🔥

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
                    // Por seguridad, no revelamos si el email existe o no
                    return Ok(new ForgotPasswordResponse
                    {
                        IsSuccess = true,
                        Message = "Si el email existe en nuestro sistema, recibirás un código de recuperación."
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
                    usuario.Nombre,
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
            catch (Exception ex)
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
            catch (Exception ex)
            {
                return StatusCode(500, new ForgotPasswordResponse
                {
                    IsSuccess = false,
                    Message = "Error interno del servidor."
                });
            }
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

        // Método auxiliar para generar token
        private string GenerateRecoveryToken()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString(); // 6 dígitos
        }
    }
}