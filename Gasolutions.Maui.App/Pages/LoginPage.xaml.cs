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

            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            var isLoggedIn = await _authService.CheckAuthStatus();

            if (isLoggedIn)
            {
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

            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            ErrorLabel.IsVisible = false;

            var loginButton = (Button)sender;
            loginButton.IsEnabled = false;

            try
            {
                var response = await _authService.Login(EmailEntry.Text, PasswordEntry.Text);

                if (response.Success)
                {
                    await NavigateToMainPage();
                }
                else
                {
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
            EmailEntry.Text = string.Empty;
            PasswordEntry.Text = string.Empty;

            if (AuthService.CurrentUser.IsAdmin)
            {
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