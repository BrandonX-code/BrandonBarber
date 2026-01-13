using CommunityToolkit.Maui;
using Microcharts.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Platform;
using Plugin.LocalNotification;
using Microsoft.Maui.LifecycleEvents;

#if ANDROID
using Plugin.Firebase.Core.Platforms.Android;
using Android;
using Android.App;
using Android.Content.PM;
using AndroidX.Core.App;
using AndroidX.Core.Content;
#endif


namespace Barber.Maui.BrandonBarber
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
            .RegisterFirebaseServices();
            builder
           .UseMauiCommunityToolkit(options =>
           {
               options.SetShouldEnableSnackbarOnWindows(true);
           })
           .UseMauiCommunityToolkit()
           .UseMicrocharts()
           .ConfigureFonts(fonts =>
           {
               fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
               fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
           });
            builder.ConfigureLifecycleEvents(events =>
            {
#if ANDROID
                events.AddAndroid(android =>
                {
                    android.OnCreate((activity, bundle) =>
                    {
                        SolicitarPermisoNotificaciones(activity);
                    });
                });
#endif
            });


#if ANDROID
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (h, v) =>
            {
                h.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Colors.Transparent.ToPlatform());
            });
#endif

            // ✅ CONFIGURACIÓN ACTUALIZADA PARA RENDER
            string apiBaseUrl;

#if DEBUG
            // En modo DEBUG: usa localhost o IP local
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                apiBaseUrl = "https://192.168.0.135:7283/"; // Para emulador/dispositivo Android en red local
            }
            else
            {
                apiBaseUrl = "https://localhost:7283/"; // Para Windows/iOS en desarrollo
            }
#else
            // En modo RELEASE: usa la URL de Render (producción)
            apiBaseUrl = "https://brandonbarber.onrender.com/";
#endif

            builder.Services.AddSingleton<HttpClient>(sp =>
            {
                var httpClientHandler = new HttpClientHandler();

#if DEBUG
                // Solo ignora certificados SSL en desarrollo
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    httpClientHandler.ServerCertificateCustomValidationCallback =
                        (message, cert, chain, errors) => true;
                }
#endif

                var httpClient = new HttpClient(httpClientHandler)
                {
                    BaseAddress = new Uri(apiBaseUrl),
                    Timeout = TimeSpan.FromSeconds(30) // ✅ Aumenta el timeout para Render
                };

                return httpClient;
            });

            // Registrar servicios
            builder.Services.AddSingleton<ReservationService>();
            builder.Services.AddSingleton<PerfilUsuarioService>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<DisponibilidadService>();
            builder.Services.AddSingleton<GaleriaService>();
            builder.Services.AddSingleton<ServicioService>();
            builder.Services.AddSingleton<CalificacionService>();
            builder.Services.AddSingleton<BarberiaService>();
            builder.Services.AddSingleton<AdministradorService>();
            builder.Services.AddSingleton<NotificationService>();
            builder.Services.AddSingleton<UpdateService>();
            LocalNotificationCenter.Current.NotificationActionTapped += (e) =>
            {
                Console.WriteLine($"📱 Notificación tocada: {e.Request.NotificationId}");
            };

            // Registrar páginas
            builder.Services.AddSingleton<LoginPage>();
            builder.Services.AddTransient<RegistroPage>();
            builder.Services.AddSingleton<BuscarPage>();
            builder.Services.AddSingleton<ListaCitas>();
            builder.Services.AddSingleton<PerfilPage>();
            builder.Services.AddSingleton<GaleriaPage>();
            builder.Services.AddSingleton<SeleccionBarberiaPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }

        private static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
        {
            builder.ConfigureLifecycleEvents(events =>
            {
#if IOS
                    events.AddiOS(iOS => iOS.WillFinishLaunching((_, __) => {
                        CrossFirebase.Initialize();
                        FirebaseCloudMessagingImplementation.Initialize();
                        return false;
                    }));
#elif ANDROID
                events.AddAndroid(android => android.OnCreate((activity, _) =>
                CrossFirebase.Initialize(activity, () => activity)));
#endif
            });

            return builder;
        }
#if ANDROID
        public static void SolicitarPermisoNotificaciones(Android.App.Activity activity)
        {
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
            {
                if (ContextCompat.CheckSelfPermission(
                        activity,
                        Manifest.Permission.PostNotifications)
                    != Permission.Granted)
                {
                    ActivityCompat.RequestPermissions(
                        activity,
                        new[] { Manifest.Permission.PostNotifications },
                        1001
                    );
                }
            }
        }
#endif


    }
}