using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Barber.Maui.API.Pages
{
    [IgnoreAntiforgeryToken]
    public class AdminRegisterModel : PageModel
    {
        private readonly AppDbContext _context;

        public AdminRegisterModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int SolicitudId { get; set; }

        [BindProperty]
        public string? Contrasena { get; set; }

        [BindProperty]
        public string? ConfirmarContrasena { get; set; }


        public SolicitudAdmin? Solicitud { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (SolicitudId <= 0)
            {
                ErrorMessage = "Solicitud no válida";
                return Page();
            }

            Solicitud = await _context.SolicitudesAdmin
                .FirstOrDefaultAsync(s => s.Id == SolicitudId && s.Estado == "Aprobado" && !s.RegistroCompletado);

            if (Solicitud == null)
            {
                ErrorMessage = "Solicitud no encontrada, ya completada o no aprobada";
                return Page();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Log para debug
                Console.WriteLine($"=== INICIO POST ===");
                Console.WriteLine($"SolicitudId: {SolicitudId}");
                Console.WriteLine($"Contraseña: {(string.IsNullOrEmpty(Contrasena) ? "vacía" : "tiene valor")}");
                Console.WriteLine($"ConfirmarContraseña: {(string.IsNullOrEmpty(ConfirmarContrasena) ? "vacía" : "tiene valor")}");

                if (string.IsNullOrWhiteSpace(Contrasena) || string.IsNullOrWhiteSpace(ConfirmarContrasena))
                {
                    ErrorMessage = "Todos los campos son obligatorios";
                    await LoadDataAsync();
                    return Page();
                }

                if (Contrasena != ConfirmarContrasena)
                {
                    ErrorMessage = "Las contraseñas no coinciden";
                    await LoadDataAsync();
                    return Page();
                }

                if (Contrasena.Length < 6)
                {
                    ErrorMessage = "La contraseña debe tener al menos 6 caracteres";
                    await LoadDataAsync();
                    return Page();
                }

                var solicitud = await _context.SolicitudesAdmin
                    .FirstOrDefaultAsync(s => s.Id == SolicitudId && s.Estado == "Aprobado" && !s.RegistroCompletado);

                if (solicitud == null)
                {
                    ErrorMessage = "Solicitud no válida o ya completada";
                    await LoadDataAsync();
                    return Page();
                }
                if (await _context.UsuarioPerfiles.AnyAsync(u => u.Email == solicitud.EmailSolicitante))
                {
                    ErrorMessage = "Ya existe un usuario registrado con este correo electrónico.";
                    await LoadDataAsync();
                    return Page();
                }


                // Verificar si ya existe un usuario con esta cédula
                var usuarioExistente = await _context.UsuarioPerfiles
                    .AnyAsync(u => u.Cedula == solicitud.CedulaSolicitante);

                if (usuarioExistente)
                {
                    ErrorMessage = "Ya existe un usuario con esta cédula";
                    await LoadDataAsync();
                    return Page();
                }

                // Crear nuevo administrador
                var nuevoAdmin = new Auth
                {
                    Cedula = solicitud.CedulaSolicitante,
                    Nombre = solicitud.NombreSolicitante,
                    Email = solicitud.EmailSolicitante,
                    Telefono = solicitud.TelefonoSolicitante,
                    Contraseña = Contrasena,
                    Rol = "administrador",
                    IdBarberia = null
                };

                _context.UsuarioPerfiles.Add(nuevoAdmin);
                solicitud.RegistroCompletado = true;
                await _context.SaveChangesAsync();

                Console.WriteLine("✅ Usuario creado exitosamente");

                SuccessMessage = "¡Registro completado! Ya puedes iniciar sesión en la aplicación.";
                Solicitud = null;
                Contrasena = null;
                ConfirmarContrasena = null;
                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                ErrorMessage = $"Error al procesar el registro: {ex.Message}";
                await LoadDataAsync();
                return Page();
            }
        }

        private async Task LoadDataAsync()
        {
            Solicitud = await _context.SolicitudesAdmin
                .FirstOrDefaultAsync(s => s.Id == SolicitudId);
        }
    }
}