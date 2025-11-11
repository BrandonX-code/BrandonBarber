using Microsoft.Maui.Controls;
using System;
using Barber.Maui.BrandonBarber.Pages;
using Barber.Maui.BrandonBarber.Services;

namespace Barber.Maui.BrandonBarber
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Registrar rutas para la navegación
            Routing.RegisterRoute("login", typeof(LoginPage));
            Routing.RegisterRoute("registro", typeof(RegistroPage));
            Routing.RegisterRoute("perfil", typeof(PerfilPage));
            Routing.RegisterRoute("editarPerfil", typeof(EditarPerfilPage));
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Siempre iniciar con LoginPage, que decidirá si redirigir
            return new Window(new NavigationPage(new LoginPage()));
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