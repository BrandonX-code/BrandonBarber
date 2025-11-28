using Barber.Maui.API.Data;
using Barber.Maui.API.Models;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;

namespace Barber.Maui.API.Services
{
    public class FirebaseNotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private static readonly object _lock = new object();
        private static bool _initialized = false;

        public FirebaseNotificationService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;

            lock (_lock)
            {
                if (!_initialized)
                {
                    try
                    {
                        if (FirebaseApp.DefaultInstance == null)
                        {
                            // 1️⃣ Intentar leer desde la variable de entorno (Render)
                            var firebaseJson = configuration["FIREBASE_ADMIN_CREDENTIALS"];

                            GoogleCredential credential;

                            if (!string.IsNullOrWhiteSpace(firebaseJson))
                            {
                                Console.WriteLine("🔐 Credenciales Firebase desde variable de entorno");
                                credential = GoogleCredential.FromJson(firebaseJson);
                            }
                            else
                            {
                                // 2️⃣ Si estás en LOCAL usar archivo JSON
                                Console.WriteLine("📁 Cargando credenciales Firebase desde archivo local");

                                credential = GoogleCredential.FromFile("firebase-adminsdk.json");
                            }

                            FirebaseApp.Create(new AppOptions
                            {
                                Credential = credential
                            });

                            Console.WriteLine("✅ Firebase inicializado correctamente");
                        }

                        _initialized = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error inicializando Firebase: {ex.Message}");
                        throw;
                    }
                }
            }
        }


        public async Task<bool> EnviarNotificacionAsync(long usuarioCedula, string titulo, string mensaje, Dictionary<string, string>? data = null)
        {
            try
            {
                var tokens = await _context.FcmToken
                    .Where(t => t.UsuarioCedula == usuarioCedula)
                    .Select(t => t.Token)
                    .ToListAsync();

                if (!tokens.Any())
                {
                    Console.WriteLine($"⚠️ No hay tokens para usuario {usuarioCedula}");
                    return false;
                }

                var message = new MulticastMessage
                {
                    Tokens = tokens,
                    Notification = new Notification
                    {
                        Title = titulo,
                        Body = mensaje,
                        ImageUrl = "https://i.pinimg.com/736x/74/2e/a6/742ea6bccad14b6b92535cd27f3e1f10.jpg" // 🔥 MOVER AQUÍ
                    },
                    Data = data ?? new Dictionary<string, string>(),
                    Android = new AndroidConfig
                    {
                        Notification = new AndroidNotification
                        {
                            Color = "#0E2A36", // 🔥 Color de tu marca
                            Sound = "default",
                            ChannelId = "barber_notifications"
                            // 🔥 QUITAR ImageUrl de aquí
                        }
                    }
                };

                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
                Console.WriteLine($"✅ Notificación enviada: {response.SuccessCount}/{tokens.Count}");

                return response.SuccessCount > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error enviando notificación: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RegistrarTokenAsync(long usuarioCedula, string token)
        {
            try
            {
                // 1. Eliminar cualquier token igual asignado a otro usuario
                var tokensDuplicados = await _context.FcmToken
                    .Where(t => t.Token == token && t.UsuarioCedula != usuarioCedula)
                    .ToListAsync();

                if (tokensDuplicados.Any())
                    _context.FcmToken.RemoveRange(tokensDuplicados);

                // 2. Eliminar TODOS los tokens anteriores del usuario
                var tokensUsuario = await _context.FcmToken
                    .Where(t => t.UsuarioCedula == usuarioCedula)
                    .ToListAsync();

                if (tokensUsuario.Any())
                    _context.FcmToken.RemoveRange(tokensUsuario);

                // 3. Guardar SOLO el token nuevo
                _context.FcmToken.Add(new FcmToken
                {
                    UsuarioCedula = usuarioCedula,
                    Token = token,
                    UltimaActualizacion = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error registrando token: {ex.Message}");
                return false;
            }
        }


    }
}