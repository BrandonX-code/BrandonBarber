namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class SplashPage : ContentPage
    {
        private readonly AuthService _authService;

        public SplashPage()
        {
            InitializeComponent();
            _authService = Application.Current!.Handler.MauiContext!.Services.GetService<AuthService>()!;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

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