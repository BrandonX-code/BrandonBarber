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

                Console.WriteLine($"🔍 Buscando tokens para usuario {usuarioCedula}: {tokens.Count} encontrados");

                if (!tokens.Any())
                {
                    Console.WriteLine($"⚠️ NO hay tokens registrados para usuario {usuarioCedula}");
                    Console.WriteLine($"   💡 El cliente probablemente NO ha instalado la app o NO otorgó permisos de notificación");
                    return false;
                }

                // ✅ Mostrar tokens encontrados (primeros caracteres)
                foreach (var token in tokens)
                {
                    Console.WriteLine($"   ✓ Token: {token.Substring(0, 30)}...");
                }

                // ✅ AGREGAR DATOS OBLIGATORIOS
                var datosNotificacion = data ?? new Dictionary<string, string>();
                if (!datosNotificacion.ContainsKey("tipo"))
                {
                    datosNotificacion["tipo"] = "cita";
                }

                var message = new MulticastMessage
                {
                    Tokens = tokens,
                    Notification = new Notification
                    {
                        Title = titulo,
                        Body = mensaje
                    },
                    Data = datosNotificacion,
                    // ✅ CONFIGURACIÓN CRÍTICA PARA VELOCIDAD
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High, // ✅ MÁXIMA PRIORIDAD
                        Notification = new AndroidNotification
                        {
                            Color = "#0E2A36",
                            Sound = "default",
                            ChannelId = "barber_notifications",
                            Icon = "barber_notification",
                            ImageUrl = null,
                            // ✅ AGREGAR VIBRACIÓN INMEDIATA
                            VibrateTimingsMillis = new long[] { 0, 100, 100, 100 },
                            LocalOnly = false,
                            Tag = $"cita_{usuarioCedula}_{DateTime.UtcNow.Ticks}", // Evitar duplicados
                        }
                    },
                    // ✅ CONFIGURACIÓN PARA IOS
                    Apns = new ApnsConfig
                    {
                        Headers = new Dictionary<string, string>
                        {
                            { "apns-priority", "10" }, // ✅ MÁXIMA PRIORIDAD EN IOS
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
                            Badge = 1, // ✅ CORREGIR A INT
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

                Console.WriteLine($"📤 Enviando notificación a {tokens.Count} dispositivo(s)");
                Console.WriteLine($"   Título: {titulo}");
                Console.WriteLine($"   Mensaje: {mensaje.Substring(0, Math.Min(50, mensaje.Length))}...");
                Console.WriteLine($"   Timestamp: {DateTime.Now:HH:mm:ss.fff}");

                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
                
                Console.WriteLine($"✅ RESULTADO: {response.SuccessCount}/{tokens.Count} enviadas exitosamente");

                // ✅ LOG DETALLADO DE ERRORES
                if (response.FailureCount > 0)
                {
                    Console.WriteLine($"⚠️ {response.FailureCount} notificaciones fallaron:");
                    for (int i = 0; i < response.Responses.Count; i++)
                    {
                        if (!response.Responses[i].IsSuccess)
                        {
                            Console.WriteLine($"   ❌ Token {i}: {response.Responses[i].Exception?.Message}");
                        }
                    }
                }

                return response.SuccessCount > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error enviando notificación: {ex.Message}");
                Console.WriteLine($"❌ Stack Trace: {ex.StackTrace}");
                return false;
            }
        }
        public async Task<bool> RegistrarTokenAsync(long usuarioCedula, string token)
        {
            try
            {
                Console.WriteLine($"\n📝 === REGISTRANDO TOKEN FCM ===");
                Console.WriteLine($"   Usuario: {usuarioCedula}");
                Console.WriteLine($"   Token: {token.Substring(0, 30)}...");
                Console.WriteLine($"   Timestamp: {DateTime.Now:HH:mm:ss.fff}");

                // 1. Eliminar cualquier token igual asignado a otro usuario
                var tokensDuplicados = await _context.FcmToken
                    .Where(t => t.Token == token && t.UsuarioCedula != usuarioCedula)
                    .ToListAsync();

                if (tokensDuplicados.Any())
                {
                    Console.WriteLine($"   🗑️ Eliminando {tokensDuplicados.Count} tokens duplicados en otros usuarios");
                    _context.FcmToken.RemoveRange(tokensDuplicados);
                }

                // 2. Eliminar TODOS los tokens anteriores del usuario
                var tokensUsuario = await _context.FcmToken
                    .Where(t => t.UsuarioCedula == usuarioCedula)
                    .ToListAsync();

                if (tokensUsuario.Any())
                {
                    Console.WriteLine($"   🗑️ Eliminando {tokensUsuario.Count} tokens antiguos del usuario");
                    _context.FcmToken.RemoveRange(tokensUsuario);
                }

                // 3. Guardar SOLO el token nuevo
                var nuevoToken = new FcmToken
                {
                    UsuarioCedula = usuarioCedula,
                    Token = token,
                    FechaRegistro = DateTime.UtcNow,
                    UltimaActualizacion = DateTime.UtcNow
                };

                _context.FcmToken.Add(nuevoToken);
                await _context.SaveChangesAsync();

                Console.WriteLine($"   ✅ Token registrado exitosamente");
                Console.WriteLine($"   ✓ Usuario: {usuarioCedula}");
                Console.WriteLine($"   ✓ Registro: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"   ✓ Estado: ACTIVO Y LISTO PARA NOTIFICACIONES\n");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error registrando token: {ex.Message}");
                Console.WriteLine($"   ❌ Stack: {ex.StackTrace}");
                return false;
            }
        }


    }
}