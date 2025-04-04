using CommunityToolkit.Maui;
using Gasolutions.Maui.App.Pages;
using Gasolutions.Maui.App.Services;
using Microsoft.Extensions.Logging;

namespace Gasolutions.Maui.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
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

            builder.Services.AddSingleton<ReservationService>();
            builder.Services.AddSingleton<BuscarPage>();
            builder.Services.AddSingleton<ListaCitas>();
            builder.UseMauiApp<App>().UseMauiCommunityToolkit();


#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
