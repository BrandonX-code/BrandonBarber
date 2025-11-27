using Plugin.Firebase.CloudMessaging;

namespace Barber.Maui.BrandonBarber
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            CrossFirebaseCloudMessaging.Current.TokenChanged += (s, e) =>
            {
                Console.WriteLine("🔥 FCM TOKEN: " + e.Token);
            };

            CrossFirebaseCloudMessaging.Current.NotificationReceived += (s, e) =>
            {
                Console.WriteLine("📩 Notificación recibida: " + e.Notification.Body);
            };
            // Registrar rutas para la navegación
            Routing.RegisterRoute("login", typeof(LoginPage));
            Routing.RegisterRoute("registro", typeof(RegistroPage));
            Routing.RegisterRoute("perfil", typeof(PerfilPage));
            Routing.RegisterRoute("editarPerfil", typeof(EditarPerfilPage));
        }
        protected override Window CreateWindow(IActivationState? activationState)
        {
            Console.WriteLine("🔷 App - Iniciando con SplashPage");
            return new Window(new NavigationPage(new SplashPage()));
        }
        protected override void OnStart()
        {
            base.OnStart();
        }

        protected override void OnAppLinkRequestReceived(Uri uri)
        {
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var solicitudId = query["solicitudId"];

            if (uri.AbsolutePath.Contains("admin-register") && !string.IsNullOrEmpty(solicitudId))
            {
                Shell.Current.GoToAsync($"/admin-register?solicitudId={solicitudId}");
            }

            base.OnAppLinkRequestReceived(uri);
        }
    }
}