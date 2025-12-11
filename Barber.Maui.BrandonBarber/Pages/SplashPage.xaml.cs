using Barber.Maui.BrandonBarber.Controls;
using Barber.Maui.BrandonBarber.Services;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class SplashPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly NotificationService _notificationService;
        private readonly UpdateService _updateService;

        public SplashPage(
        AuthService authService,
        NotificationService notificationService,
        UpdateService updateService)
        {
            InitializeComponent();

            _authService = authService;
            _notificationService = notificationService;
            _updateService = updateService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // ✅ INICIALIZAR FIREBASE PRIMERO
            await InicializarFirebase();

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

                    var currentVersion = VersionTracking.CurrentVersion;
                    var popup = new UpdateAlertPopup(
                        updateInfo.Mensaje,
                        updateInfo.ApkUrl,
                        currentVersion,        // ← Versión actual
                        updateInfo.Version     // ← Nueva versión
                    );
                    await popup.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error al verificar actualización: {ex.Message}");
            }
        }

        // ✅ MÉTODO ÚNICO Y CORRECTO PARA NAVEGAR
        private void NavigateToMainPage()
        {
            try
            {
                var serviciosService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ServicioService>();
                // ✅ Envolver en NavigationPage para mantener consistencia
                var mainPage = new NavigationPage(new InicioPages(_authService, serviciosService));

                // ✅ USAR SOLO Application.Current.MainPage
                Application.Current!.MainPage = mainPage;

                Console.WriteLine("✅ Navegado a MainPage correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error navegando a MainPage: {ex.Message}");
                NavigateToLoginPage();
            }
        }

        // ✅ MÉTODO ÚNICO Y CORRECTO PARA NAVEGAR AL LOGIN
        private void NavigateToLoginPage()
        {
            try
            {
                var loginPage = new NavigationPage(new LoginPage());

                // ✅ USAR SOLO Application.Current.MainPage
                Application.Current!.MainPage = loginPage;

                Console.WriteLine("✅ Navegado a LoginPage correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error navegando a LoginPage: {ex.Message}");
            }
        }

        // ✅ NUEVO MÉTODO: Inicializar Firebase
        private async Task InicializarFirebase()
        {
            try
            {
                Console.WriteLine("🔥 Inicializando Firebase...");
                await _notificationService.InicializarAsync();
                Console.WriteLine("✅ Firebase inicializado correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error inicializando Firebase (no crítico): {ex.Message}");
                // No lanzar excepción, Firebase es opcional
            }
        }

        // ✅ NUEVO MÉTODO: Registrar token con reintentos
        private async Task RegistrarTokenConReintentos()
        {
            const int maxIntentos = 3;
            int intento = 0;

            while (intento < maxIntentos)
            {
                try
                {
                    intento++;
                    Console.WriteLine($"📱 Intentando registrar token FCM ({intento}/{maxIntentos})...");

                    // Esperar a que Firebase esté realmente listo
                    await Task.Delay(500);

                    var fcmToken = await Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.GetTokenAsync();

                    if (!string.IsNullOrEmpty(fcmToken))
                    {
                        var request = new
                        {
                            UsuarioCedula = AuthService.CurrentUser!.Cedula,
                            FcmToken = fcmToken
                        };

                        var json = System.Text.Json.JsonSerializer.Serialize(request);
                        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                        var httpClient = App.Current!.Handler.MauiContext!.Services
                            .GetRequiredService<AuthService>()
                            .GetType()
                            .GetField("_BaseClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            ?.GetValue(App.Current!.Handler.MauiContext!.Services.GetRequiredService<AuthService>()) as HttpClient;

                        if (httpClient != null)
                        {
                            var response = await httpClient.PostAsync("api/notifications/register-token", content);

                            if (response.IsSuccessStatusCode)
                            {
                                Console.WriteLine("✅ Token FCM registrado exitosamente");
                                return; // ✅ ÉXITO - Salir
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Intento {intento} falló: {ex.Message}");

                    if (intento < maxIntentos)
                    {
                        // Esperar más tiempo antes del siguiente intento
                        await Task.Delay(1000 * intento); // 1s, 2s, 3s...
                    }
                }
            }

            Console.WriteLine($"⚠️ No se pudo registrar token FCM después de {maxIntentos} intentos");
            // No lanzar excepción - permitir que la app continúe
        }
    }
}