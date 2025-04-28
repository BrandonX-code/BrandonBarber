using CommunityToolkit.Maui;
using Gasolutions.Maui.App.Pages;
using Gasolutions.Maui.App.Services;
using Microcharts.Maui;
using Microsoft.Extensions.Logging;

namespace Gasolutions.Maui.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>();
            builder
           .UseMauiCommunityToolkit(options =>
           {
               options.SetShouldEnableSnackbarOnWindows(true); // 👈 Habilita Snackbar en Windows
           })
           .UseMauiCommunityToolkit()
           .UseMicrocharts()
           .ConfigureFonts(fonts =>
           {
               fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
               fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
           });

            string apiBaseUrl;
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                apiBaseUrl = "https://192.168.0.155:7283/api/citas";
            }
            else
            {
                apiBaseUrl = "https://localhost:7283/api/citas";
            }

            builder.Services.AddSingleton<HttpClient>(sp =>
            {
                var httpClientHandler = new HttpClientHandler();
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    httpClientHandler.ServerCertificateCustomValidationCallback =
                        (message, cert, chain, errors) => true;
                }
                return new HttpClient(httpClientHandler) { BaseAddress = new Uri(apiBaseUrl) };
            });

            // Registrar servicios
            builder.Services.AddSingleton<ReservationService>();
            builder.Services.AddSingleton<PerfilUsuarioService>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<DisponibilidadService>();

            // Registrar páginas
            builder.Services.AddSingleton<LoginPage>();
            builder.Services.AddTransient<RegistroPage>();
            builder.Services.AddSingleton<BuscarPage>();
            builder.Services.AddSingleton<ListaCitas>();
            builder.Services.AddSingleton<PerfilPage>();

            // Registrar páginas de administrador
            //builder.Services.AddTransient<AdminDashboardPage>();
            //builder.Services.AddTransient<AdminBarberosPage>();
            //builder.Services.AddTransient<AdminReportesPage>();

            builder.UseMauiApp<App>().UseMauiCommunityToolkit();

#if DEBUG
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }
    }
}