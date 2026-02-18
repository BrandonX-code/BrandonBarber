using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.CloudMessaging.EventArgs;
using Plugin.LocalNotification;
using System.Net.Http.Json;
using Barber.Maui.BrandonBarber.Pages;

namespace Barber.Maui.BrandonBarber.Services
{
    public class NotificationService
    {
        private readonly HttpClient _httpClient;
        private static bool _initialized = false;

        public NotificationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task InicializarAsync()
        {
            if (_initialized) return;

            try
            {
                Console.WriteLine("🔥 Iniciando Firebase Cloud Messaging...");

                await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();

                var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
                Console.WriteLine($"🔥 FCM Token obtenido: {token.Substring(0, 20)}...");

                // ✅ REGISTRAR TOKEN INMEDIATAMENTE (NO ESPERAR)
                if (AuthService.CurrentUser != null)
                {
                    // Lanzar sin esperar para no bloquear la inicialización
                    _ = RegistrarTokenEnServidor(token);
                }

                // ✅ SUSCRIBIRSE A NOTIFICACIONES - ESTO DEBE SER INMEDIATO
                CrossFirebaseCloudMessaging.Current.NotificationReceived += OnNotificationReceived;
                CrossFirebaseCloudMessaging.Current.TokenChanged += OnTokenChanged;

                // ✅ MANEJAR CLIC EN NOTIFICACIONES
                LocalNotificationCenter.Current.NotificationActionTapped += async (eventArgs) =>
                {
                    Console.WriteLine($"📲 Notificación tocada: {eventArgs.Request.NotificationId}");

                    try
                    {
                        var tipo = "cita";
                        var usuario = AuthService.CurrentUser;

                        if (usuario != null)
                        {
                            Console.WriteLine($"📲 Navegando por notificación (Rol: {usuario.Rol})");
                            await NavigarSegunNotificacion(tipo);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error al procesar clic: {ex.Message}");
                    }
                };

                _initialized = true;
                Console.WriteLine("✅ Firebase Cloud Messaging inicializado correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error inicializando notificaciones: {ex.Message}");
            }
        }

        private async void OnNotificationReceived(object? sender, FCMNotificationReceivedEventArgs e)
        {
            Console.WriteLine($"📩 Notificación RECIBIDA (Firebase): {e.Notification.Title}");
            Console.WriteLine($"📩 Mensaje: {e.Notification.Body}");
            Console.WriteLine($"📩 Timestamp: {DateTime.Now:HH:mm:ss.fff}");

            // ✅ EXTRAER DATOS
            string tipo = "cita";
            if (e.Notification.Data?.ContainsKey("tipo") == true)
            {
                tipo = e.Notification.Data["tipo"];
            }

            // ✅ MOSTRAR NOTIFICACIÓN LOCAL INMEDIATAMENTE
            var notification = new NotificationRequest
            {
                NotificationId = Random.Shared.Next(1, 10000),
                Title = e.Notification.Title ?? "Notificación",
                Description = e.Notification.Body ?? "Tienes una nueva notificación",
                CategoryType = NotificationCategoryType.Status
            };

            // ✅ MOSTRAR NOTIFICACIÓN EN EL HILO PRINCIPAL
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await LocalNotificationCenter.Current.Show(notification);
                    Console.WriteLine($"✅ Notificación local mostrada al usuario");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error mostrando notificación local: {ex.Message}");
                }
            });

            // ✅ ACTUALIZAR UI MESSENGER
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new NotificacionRecibidaMessage(tipo));
            });
        }

        // ✅ NUEVO MÉTODO: NAVEGAR SEGÚN TIPO DE NOTIFICACIÓN Y ROL
        private async Task NavigarSegunNotificacion(string tipo)
        {
            try
            {
                var usuario = AuthService.CurrentUser;
                if (usuario == null)
                {toerio 
                    Console.WriteLine("⚠️ Usuario no autenticado, no se puede navegar");
                    return;
                }

                // ✅ VALIDAR QUE LA APP ESTÉ LISTA
                if (Application.Current?.MainPage == null)
                {
                    Console.WriteLine("⚠️ MainPage no está disponible");
                    return;
                }

                // Obtener la página actual
                var navigationPage = Application.Current.MainPage as NavigationPage;
                if (navigationPage == null)
                {
                    Console.WriteLine("⚠️ No hay NavigationPage");
                    return;
                }

                // ✅ NAVEGAR SEGÚN EL ROL DEL USUARIO
                if (usuario.Rol!.Equals("barbero", StringComparison.OrdinalIgnoreCase))
                {
                    // BARBERO -> GestionarCitasBarberoPage
                    if (tipo.Contains("cita", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("🎯 Navegando a GestionarCitasBarberoPage (Barbero)");
                        var reservationService = App.Current!.Handler.MauiContext!.Services
                            .GetRequiredService<ReservationService>();

                        await navigationPage.Navigation.PushAsync(
                   new GestionarCitasBarberoPage(reservationService)
                        );
                    }
                }
                else if (usuario.Rol!.Equals("cliente", StringComparison.OrdinalIgnoreCase))
                {
                    // CLIENTE -> BuscarPage
                    if (tipo.Contains("cita", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("🎯 Navegando a BuscarPage (Cliente)");
                        var reservationService = App.Current!.Handler.MauiContext!.Services
                           .GetRequiredService<ReservationService>();

                        await navigationPage.Navigation.PushAsync(
                         new BuscarPage(reservationService)
                           );
                    }
                }
                else
                {
                    Console.WriteLine($"ℹ️ Rol no configurado para navegación: {usuario.Rol}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al navegar: {ex.Message}");
            }
        }

        private async void OnTokenChanged(object? sender, FCMTokenChangedEventArgs e)
        {
            Console.WriteLine($"🔄 Token actualizado: {e.Token}");

            if (AuthService.CurrentUser != null)
            {
                await RegistrarTokenEnServidor(e.Token);
            }
        }

        public async Task RegistrarTokenEnServidor(string token)
        {
            try
            {
                var request = new
                {
                    UsuarioCedula = AuthService.CurrentUser!.Cedula,
                    FcmToken = token
                };

                var response = await _httpClient.PostAsJsonAsync("api/notifications/register-token", request);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ Token registrado en servidor");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error registrando token: {ex.Message}");
            }
        }
    }

    // Mensaje para actualizar UI
    public class NotificacionRecibidaMessage
    {
        public string Tipo { get; }
        public NotificacionRecibidaMessage(string tipo) => Tipo = tipo;
    }
}