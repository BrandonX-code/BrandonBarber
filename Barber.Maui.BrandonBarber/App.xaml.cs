namespace Barber.Maui.BrandonBarber
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();

            // Registrar rutas para la navegación
            Routing.RegisterRoute("login", typeof(LoginPage));
            Routing.RegisterRoute("registro", typeof(RegistroPage));
            Routing.RegisterRoute("perfil", typeof(PerfilPage));
            Routing.RegisterRoute("editarPerfil", typeof(EditarPerfilPage));
        }

        protected override async void OnStart()
        {
            base.OnStart();

            // Redirigir a la página de login si no hay una sesión activa
            var authService = Current.Handler.MauiContext.Services.GetService<AuthService>();
            var isLoggedIn = await authService.CheckAuthStatus();

            if (!isLoggedIn)
            {
                MainPage = new NavigationPage(new LoginPage());
            }
        }
    }
}