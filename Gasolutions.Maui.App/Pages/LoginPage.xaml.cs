using Gasolutions.Maui.App.Models;
using Gasolutions.Maui.App.Services;
using System;
using System.Threading.Tasks;

namespace Gasolutions.Maui.App.Pages
{
    public partial class LoginPage : ContentPage
    {
        private readonly AuthService _authService;

        public LoginPage()
        {
            InitializeComponent();
            _authService = Application.Current.Handler.MauiContext.Services.GetService<AuthService>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Verificar si ya hay una sesión activa
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            var isLoggedIn = await _authService.CheckAuthStatus();

            if (isLoggedIn)
            {
                // Ya está autenticado, redirigir a la página principal
                await NavigateToMainPage();
            }

            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EmailEntry.Text) || string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                ErrorLabel.Text = "Por favor, completa todos los campos";
                ErrorLabel.IsVisible = true;
                return;
            }

            // Mostrar indicador de carga
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            ErrorLabel.IsVisible = false;

            // Deshabilitar botones durante la operación
            var loginButton = (Button)sender;
            loginButton.IsEnabled = false;

            try
            {
                var response = await _authService.Login(EmailEntry.Text, PasswordEntry.Text);

                if (response.Success)
                {
                    // Login exitoso, redirigir a la página principal
                    await NavigateToMainPage();
                }
                else
                {
                    // Mostrar error
                    ErrorLabel.Text = response.Message;
                    ErrorLabel.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = $"Error: {ex.Message}";
                ErrorLabel.IsVisible = true;
            }
            finally
            {
                // Restablecer la UI
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
                loginButton.IsEnabled = true;
            }
        }

        private async void OnRegistroClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegistroPage());
        }

        private async Task NavigateToMainPage()
        {
            // Limpiar los campos
            EmailEntry.Text = string.Empty;
            PasswordEntry.Text = string.Empty;

            if (AuthService.CurrentUser.IsAdmin)
            {
                // Alternativa a Shell.Current
                //await Application.Current.MainPage.Navigation.PushAsync(new AdminPage());
            }
            else
            {
                Preferences.Set("IsLoggedIn", true);
                Application.Current.MainPage = new NavigationPage(new InicioPages());
            }
        }
    }
}