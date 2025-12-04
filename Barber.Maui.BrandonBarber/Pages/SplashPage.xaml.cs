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

            _authService = Application.Current!.Handler!.MauiContext!.Services.GetService<AuthService>()!;
            _notificationService = Application.Current!.Handler!.MauiContext!.Services.GetService<NotificationService>()!;
            _updateService = Application.Current!.Handler!.MauiContext!.Services.GetService<UpdateService>()!;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Verificar actualización antes de cualquier otra acción
            await VerificarActualizacion();

            // Animaciones de entrada simples
            _ = AnimateElements();

            // Esperar un mínimo de tiempo para mostrar el splash (opcional, para mejor UX)
            var minSplashTime = Task.Delay(1500); // 1.5 segundos mínimo

            Console.WriteLine("🔷 SplashPage - Verificando sesión...");

            try
            {
                // Verificar si hay sesión activa
                var isLoggedIn = await _authService.CheckAuthStatus();

                // Esperar el tiempo mínimo del splash
                await minSplashTime;

                Console.WriteLine($"🔷 SplashPage - Sesión válida: {isLoggedIn}");

                if (isLoggedIn && AuthService.CurrentUser != null)
                {
                    Console.WriteLine($"🔷 Usuario: {AuthService.CurrentUser.Nombre} - Rol: {AuthService.CurrentUser.Rol}");
                    NavigateToMainPage();
                }
                else
                {
                    Console.WriteLine("🔷 No hay sesión - Redirigiendo a Login");
                    NavigateToLoginPage();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en SplashPage: {ex.Message}");
                // En caso de error, ir al login
                await minSplashTime;
                NavigateToLoginPage();
            }
        }

        private async Task AnimateElements()
        {
            // Animar logo
            await LogoImage.FadeTo(1, 600, Easing.CubicOut);
            await LoadingLabel.FadeTo(1, 400, Easing.CubicOut);
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
        private void NavigateToMainPage()
        {
            try
            {
                var serviciosService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ServicioService>();
                var mainPage = new NavigationPage(new InicioPages(_authService, serviciosService));

                if (Application.Current?.Windows.Count > 0)
                {
                    Application.Current.Windows[0].Page = mainPage;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error navegando a MainPage: {ex.Message}");
                NavigateToLoginPage();
            }
        }
        private void NavigateToLoginPage()
        {
            try
            {
                var loginPage = new NavigationPage(new LoginPage());

                if (Application.Current?.Windows.Count > 0)
                {
                    Application.Current.Windows[0].Page = loginPage;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error navegando a LoginPage: {ex.Message}");
            }
        }
    }
}