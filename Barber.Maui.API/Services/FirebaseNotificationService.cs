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
                            // Leer JSON completo de la variable de entorno
                            var firebaseJson = configuration["FIREBASE_ADMIN_CREDENTIALS"];

                            if (string.IsNullOrWhiteSpace(firebaseJson))
                                throw new Exception("No se encontró la variable de entorno FIREBASE_ADMIN_CREDENTIALS");

                            Console.WriteLine("🔐 Credenciales Firebase cargadas desde variable de entorno");

                            var credential = GoogleCredential.FromJson(firebaseJson);

                            FirebaseApp.Create(new AppOptions
                            {
                                Credential = credential
                            });

                            Console.WriteLine("✅ Firebase inicializado correctamente sin archivo");
                        }
                        else
                        {
                            Console.WriteLine("ℹ️ Firebase ya estaba inicializado");
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
                        Body = mensaje
                    },
                    Data = data ?? new Dictionary<string, string>(),
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            Sound = "default",
                            ChannelId = "barber_notifications"
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
                {
                    _context.FcmToken.RemoveRange(tokensDuplicados);
                }

                // 2. Verificar si ese mismo usuario ya tiene ese token
                var tokenExistente = await _context.FcmToken
                    .FirstOrDefaultAsync(t => t.UsuarioCedula == usuarioCedula && t.Token == token);

                if (tokenExistente != null)
                {
                    tokenExistente.UltimaActualizacion = DateTime.UtcNow;
                }
                else
                {
                    _context.FcmToken.Add(new FcmToken
                    {
                        UsuarioCedula = usuarioCedula,
                        Token = token
                    });
                }

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