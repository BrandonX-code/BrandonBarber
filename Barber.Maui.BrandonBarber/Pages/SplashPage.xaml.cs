using Barber.Maui.BrandonBarber.Controls;
using Barber.Maui.BrandonBarber.Services;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class SplashPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly NotificationService _notificationService;
        private readonly UpdateService _updateService;

        public SplashPage()
        {
            InitializeComponent();

            _authService = App.Current!.Handler!.MauiContext!.Services.GetService<AuthService>()!;
            _notificationService = App.Current!.Handler!.MauiContext!.Services.GetService<NotificationService>()!;
            _updateService = App.Current!.Handler!.MauiContext!.Services.GetService<UpdateService>()!;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                Console.WriteLine("🔷 SplashPage - OnAppearing iniciado");

                // Animación de logo
                await AnimateLogo();

                // 1️⃣ VERIFICAR ACTUALIZACIONES PRIMERO
                await VerificarActualizacion();

                // 2️⃣ Inicializar notificaciones
                await _notificationService.InicializarAsync();

                // 3️⃣ Verificar autenticación
                await VerificarAutenticacion();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en SplashPage: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    Application.Current!.MainPage = new NavigationPage(new LoginPage());
                });
            }
        }

        private async Task AnimateLogo()
        {
            await LogoImage.FadeTo(1, 1000);
            await LoadingLabel.FadeTo(1, 500);
        }

        private async Task VerificarActualizacion()
        {
            try
            {
                var updateInfo = await _updateService.CheckForUpdatesAsync();

                if (updateInfo != null)
                {
                    Console.WriteLine("🆕 Nueva versión detectada, mostrando popup");

                    var popup = new UpdateAlertPopup(updateInfo.Mensaje, updateInfo.ApkUrl);
                    await popup.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error al verificar actualización: {ex.Message}");
                // Continuar con la carga normal
            }
        }

        private async Task VerificarAutenticacion()
        {
            Console.WriteLine("🔷 Verificando autenticación...");

            bool isAuthenticated = await _authService.CheckAuthStatus();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (isAuthenticated && AuthService.CurrentUser != null)
                {
                    Console.WriteLine($"🔷 Usuario autenticado: {AuthService.CurrentUser.Nombre}");

                    // Navegar al AppShell principal (ajusta según tu estructura)
                    Application.Current!.MainPage = new AppShell();
                }
                else
                {
                    Console.WriteLine("🔷 No hay sesión activa, navegando a LoginPage");
                    Application.Current!.MainPage = new NavigationPage(new LoginPage());
                }
            });
        }
    }
}