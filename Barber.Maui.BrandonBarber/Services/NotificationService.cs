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
                await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();

                var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
                Console.WriteLine($"🔥 FCM Token: {token}");

                if (AuthService.CurrentUser != null)
                {
                    await RegistrarTokenEnServidor(token);
                }

                CrossFirebaseCloudMessaging.Current.NotificationReceived += OnNotificationReceived;
                CrossFirebaseCloudMessaging.Current.TokenChanged += OnTokenChanged;

                // ✅ MANEJAR CLIC EN NOTIFICACIONES
                LocalNotificationCenter.Current.NotificationActionTapped += async (eventArgs) =>
                {
                    Console.WriteLine($"📲 Notificación tocada: {eventArgs.Request.NotificationId}");

                    try
                    {
                        var tipo = "cita"; // Por defecto es una cita
                        var usuario = AuthService.CurrentUser;

                        if (usuario != null)
                        {
                            Console.WriteLine($"📲 Tipo de notificación: {tipo}");
                            Console.WriteLine($"👤 Rol del usuario: {usuario.Rol}");

                            // ✅ NAVEGAR SEGÚN EL TIPO Y ROL
                            await NavigarSegunNotificacion(tipo);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error al procesar clic en notificación: {ex.Message}");
                    }
                };

                _initialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error inicializando notificaciones: {ex.Message}");
            }
        }

        private async void OnNotificationReceived(object? sender, FCMNotificationReceivedEventArgs e)
        {
            Console.WriteLine($"📩 Notificación recibida: {e.Notification.Title}");

            // ✅ EXTRAER DATOS DE LA NOTIFICACIÓN
            string tipo = "default";
            if (e.Notification.Data.ContainsKey("tipo"))
            {
                tipo = e.Notification.Data["tipo"];
            }

            var notification = new NotificationRequest
            {
                NotificationId = Random.Shared.Next(),
                Title = e.Notification.Title,
                Description = e.Notification.Body,
                CategoryType = NotificationCategoryType.Status
            };

            await LocalNotificationCenter.Current.Show(notification);

            // Actualizar UI si es necesario
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
                {
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