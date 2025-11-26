using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.CloudMessaging.EventArgs;
using Plugin.LocalNotification;
using System.Net.Http.Json;

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

            var notification = new NotificationRequest
            {
                NotificationId = Random.Shared.Next(),
                Title = e.Notification.Title,
                Description = e.Notification.Body,
                CategoryType = NotificationCategoryType.Status
            };

            await LocalNotificationCenter.Current.Show(notification);

            // Actualizar UI si es necesario
            if (e.Notification.Data.ContainsKey("tipo"))
            {
                var tipo = e.Notification.Data["tipo"];

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    WeakReferenceMessenger.Default.Send(new NotificacionRecibidaMessage(tipo));
                });
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