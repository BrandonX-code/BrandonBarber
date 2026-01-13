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
                            var firebaseJson = configuration["FIREBASE_ADMIN_CREDENTIALS"];

                            GoogleCredential credential;

                            if (!string.IsNullOrWhiteSpace(firebaseJson))
                            {
                                Console.WriteLine("🔐 Credenciales Firebase desde variable de entorno");
                                credential = GoogleCredential.FromJson(firebaseJson);
                            }
                            else
                            {
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
                Console.WriteLine($"\n📤 === INICIANDO ENVÍO DE NOTIFICACIÓN ===");
                Console.WriteLine($"📤 Usuario: {usuarioCedula}");
                Console.WriteLine($"📤 Título: {titulo}");
                Console.WriteLine($"📤 Mensaje: {mensaje}");

                var tokens = await _context.FcmToken
                    .Where(t => t.UsuarioCedula == usuarioCedula)
                    .Select(t => new { t.Id, t.Token, t.FechaRegistro, t.UltimaActualizacion })
                    .ToListAsync();

                Console.WriteLine($"📤 Tokens encontrados: {tokens.Count}");

                if (!tokens.Any())
                {
                    Console.WriteLine($"⚠️ ❌ NO HAY TOKENS para usuario {usuarioCedula}");
                    Console.WriteLine($"⚠️ Este usuario probablemente:");
                    Console.WriteLine($"   - No ha iniciado sesión en la app");
                    Console.WriteLine($"   - No ha permitido notificaciones");
                    Console.WriteLine($"   - Sus tokens han sido limpios");
                    return false;
                }

                foreach (var token in tokens)
                {
                    Console.WriteLine($"  ✓ Token ID: {token.Id}");
                    Console.WriteLine($"    Token: {token.Token.Substring(0, Math.Min(30, token.Token.Length))}...");
                    Console.WriteLine($"    Registrado: {token.FechaRegistro}");
                    Console.WriteLine($"    Actualizado: {token.UltimaActualizacion}");
                }

                var datosNotificacion = data ?? new Dictionary<string, string>();
                if (!datosNotificacion.ContainsKey("tipo"))
                {
                    datosNotificacion["tipo"] = "cita";
                }

                var message = new MulticastMessage
                {
                    Tokens = tokens.Select(t => t.Token).ToList(),
                    Notification = new Notification
                    {
                        Title = titulo,
                        Body = mensaje
                    },
                    Data = datosNotificacion,
                    // ✅ CONFIGURACIÓN CRÍTICA
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            Color = "#0E2A36",
                            Sound = "default",
                            ChannelId = "barber_notifications", // ✅ IMPORTANTE: El cliente DEBE crear este canal
                            Icon = "notification_icon", // Cambié a notification_icon (más estándar)
                            VibrateTimingsMillis = new long[] { 0, 100, 100, 100 },
                            LocalOnly = false,
                            // Evitar duplicados
                            Tag = $"cita_{usuarioCedula}_{DateTime.UtcNow.Ticks}",
                        }
                    },
                    // ✅ CONFIGURACIÓN PARA IOS
                    Apns = new ApnsConfig
                    {
                        Headers = new Dictionary<string, string>
                        {
                            { "apns-priority", "10" },
                            { "apns-push-type", "alert" },
                        },
                        Aps = new Aps
                        {
                            Alert = new ApsAlert
                            {
                                Title = titulo,
                                Body = mensaje,
                            },
                            Sound = "default",
                            ContentAvailable = true,
                            MutableContent = true,
                            Badge = 1,
                        }
                    },
                    // ✅ CONFIGURACIÓN WEBPUSH
                    Webpush = new WebpushConfig
                    {
                        Data = datosNotificacion,
                        FcmOptions = new WebpushFcmOptions
                        {
                            Link = "https://barbergo.com"
                        }
                    }
                };

                Console.WriteLine($"📤 Enviando a {tokens.Count} dispositivo(s)...");
                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);

                Console.WriteLine($"✅ Resultado del envío:");
                Console.WriteLine($"   ✓ Exitosas: {response.SuccessCount}");
                Console.WriteLine($"   ✗ Fallidas: {response.FailureCount}");

                if (response.FailureCount > 0)
                {
                    Console.WriteLine($"\n⚠️ ERRORES EN {response.FailureCount} NOTIFICACIONES:");
                    for (int i = 0; i < response.Responses.Count; i++)
                    {
                        if (!response.Responses[i].IsSuccess)
                        {
                            var token = tokens[i];
                            var exception = response.Responses[i].Exception;
                            Console.WriteLine($"   ✗ Token {token.Id}: {exception?.Message}");

                            // Si es "Mismatched Credential" o "Invalid Registration Token", eliminar
                            if (exception?.Message?.Contains("Invalid") == true ||
                                exception?.Message?.Contains("Mismatched") == true)
                            {
                                Console.WriteLine($"     → Eliminando token inválido {token.Id}");
                                _context.FcmToken.Where(t => t.Id == token.Id).ExecuteDelete();
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                Console.WriteLine($"📤 === FIN ENVÍO DE NOTIFICACIÓN ===\n");

                return response.SuccessCount > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR ENVIANDO NOTIFICACIÓN:");
                Console.WriteLine($"❌ Mensaje: {ex.Message}");
                Console.WriteLine($"❌ Stack: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> RegistrarTokenAsync(long usuarioCedula, string token)
        {
            try
            {
                Console.WriteLine($"\n🔐 === REGISTRANDO TOKEN ===");
                Console.WriteLine($"🔐 Usuario: {usuarioCedula}");
                Console.WriteLine($"🔐 Token: {token.Substring(0, Math.Min(50, token.Length))}...");

                // 1. Eliminar PRIMERO tokens anteriores del mismo usuario
                var tokensAntiguos = await _context.FcmToken
                    .Where(t => t.UsuarioCedula == usuarioCedula)
                    .ToListAsync();

                if (tokensAntiguos.Any())
                {
                    Console.WriteLine($"🔐 Eliminando {tokensAntiguos.Count} token(s) anterior(es)");
                    _context.FcmToken.RemoveRange(tokensAntiguos);
                    await _context.SaveChangesAsync();
                }

                // 2. Eliminar si existe en otro usuario (duplicado)
                var tokenDuplicado = await _context.FcmToken
                    .FirstOrDefaultAsync(t => t.Token == token && t.UsuarioCedula != usuarioCedula);

                if (tokenDuplicado != null)
                {
                    Console.WriteLine($"🔐 Token existía en otro usuario ({tokenDuplicado.UsuarioCedula}), eliminando");
                    _context.FcmToken.Remove(tokenDuplicado);
                    await _context.SaveChangesAsync();
                }

                // 3. Guardar el nuevo token
                var nuevoToken = new FcmToken
                {
                    UsuarioCedula = usuarioCedula,
                    Token = token,
                    FechaRegistro = DateTime.UtcNow,
                    UltimaActualizacion = DateTime.UtcNow
                };

                _context.FcmToken.Add(nuevoToken);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Token registrado exitosamente (ID: {nuevoToken.Id})");
                Console.WriteLine($"🔐 === FIN REGISTRO TOKEN ===\n");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR REGISTRANDO TOKEN:");
                Console.WriteLine($"❌ Mensaje: {ex.Message}");
                Console.WriteLine($"❌ Stack: {ex.StackTrace}");
                return false;
            }
        }
    }
}