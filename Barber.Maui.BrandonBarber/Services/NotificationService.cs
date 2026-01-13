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
       Console.WriteLine("🔥 === INICIANDO FIREBASE CLOUD MESSAGING ===");

   await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
        Console.WriteLine("🔥 CheckIfValid completado");

       var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
       Console.WriteLine($"🔥 FCM Token obtenido: {token.Substring(0, Math.Min(50, token.Length))}...");

          // ✅ NO REGISTRAR AQUÍ - SOLO EN LOGIN/CHECKAUTH
      CrossFirebaseCloudMessaging.Current.NotificationReceived += OnNotificationReceived;
        CrossFirebaseCloudMessaging.Current.TokenChanged += OnTokenChanged;
     Console.WriteLine("✅ Listeners de notificaciones registrados");

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
         Console.WriteLine("✅ === FIREBASE CLOUD MESSAGING INICIALIZADO CORRECTAMENTE ===\n");
  }
 catch (Exception ex)
     {
   Console.WriteLine($"❌ === ERROR INICIALIZANDO NOTIFICACIONES ===");
         Console.WriteLine($"❌ Mensaje: {ex.Message}");
       Console.WriteLine($"❌ Stack: {ex.StackTrace}");
      }
        }

        private async void OnNotificationReceived(object? sender, FCMNotificationReceivedEventArgs e)
        {
   try
            {
      Console.WriteLine($"\n📩 === NOTIFICACIÓN RECIBIDA ===");
  Console.WriteLine($"📩 Título: {e.Notification.Title}");
      Console.WriteLine($"📩 Cuerpo: {e.Notification.Body}");
          Console.WriteLine($"📩 Timestamp: {DateTime.Now:HH:mm:ss.fff}");

           // ✅ EXTRAER DATOS
            string tipo = "cita";
  if (e.Notification.Data?.ContainsKey("tipo") == true)
      {
            tipo = e.Notification.Data["tipo"];
           Console.WriteLine($"📩 Tipo de notificación: {tipo}");
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
         Console.WriteLine($"✅ Mensaje enviado al messenger");
      });

    Console.WriteLine($"📩 === FIN PROCESAMIENTO DE NOTIFICACIÓN ===\n");
      }
            catch (Exception ex)
         {
      Console.WriteLine($"❌ ERROR PROCESANDO NOTIFICACIÓN:");
      Console.WriteLine($"❌ {ex.Message}");
    Console.WriteLine($"❌ {ex.StackTrace}");
          }
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
            Console.WriteLine($"\n🔄 === TOKEN ACTUALIZADO ===");
        Console.WriteLine($"🔄 Nuevo token: {e.Token.Substring(0, Math.Min(50, e.Token.Length))}...");

   if (AuthService.CurrentUser != null)
            {
    var registrado = await RegistrarTokenEnServidor(e.Token);
      if (registrado)
   {
           Console.WriteLine($"✅ Token actualizado en el servidor");
              }
        else
                {
          Console.WriteLine($"⚠️ Fallo al actualizar token en el servidor");
        }
            }
            Console.WriteLine($"🔄 === FIN ACTUALIZACIÓN TOKEN ===\n");
        }

        public async Task<bool> RegistrarTokenEnServidor(string token)
        {
         try
            {
              var usuario = AuthService.CurrentUser;
     if (usuario == null)
             {
         Console.WriteLine("⚠️ No hay usuario autenticado para registrar token");
          return false;
      }

      Console.WriteLine($"\n🔐 === REGISTRANDO TOKEN EN SERVIDOR ===");
Console.WriteLine($"🔐 Usuario: {usuario.Cedula}");
        Console.WriteLine($"🔐 Token: {token.Substring(0, Math.Min(50, token.Length))}...");

   var request = new
   {
          UsuarioCedula = usuario.Cedula,
        FcmToken = token
                };

         var response = await _httpClient.PostAsJsonAsync("api/notifications/register-token", request);

            if (response.IsSuccessStatusCode)
          {
   Console.WriteLine($"✅ Token registrado en servidor exitosamente");
Console.WriteLine($"🔐 === FIN REGISTRO TOKEN ===\n");
        return true;
     }
         else
    {
         var content = await response.Content.ReadAsStringAsync();
       Console.WriteLine($"❌ Error registrando token: {response.StatusCode}");
       Console.WriteLine($"❌ Respuesta: {content}");
      Console.WriteLine($"🔐 === FIN REGISTRO TOKEN ===\n");
  return false;
         }
        }
            catch (Exception ex)
       {
    Console.WriteLine($"❌ EXCEPCIÓN registrando token: {ex.Message}");
   Console.WriteLine($"❌ {ex.StackTrace}");
           Console.WriteLine($"🔐 === FIN REGISTRO TOKEN ===\n");
                return false;
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