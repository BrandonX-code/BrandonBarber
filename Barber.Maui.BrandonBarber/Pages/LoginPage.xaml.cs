using System.ComponentModel.DataAnnotations;
namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class LoginPage : ContentPage
    {
        private readonly AuthService _authService;

        public LoginPage()
        {
            InitializeComponent();
            _authService = Application.Current!.Handler.MauiContext!.Services.GetService<AuthService>()!;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            var isLoggedIn = await _authService.CheckAuthStatus();

            if (isLoggedIn)
            {
                NavigateToMainPage();
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

            // Validar formato del email
            var emailValidator = new EmailAddressAttribute();
            if (!emailValidator.IsValid(EmailEntry.Text))
            {
                ErrorLabel.Text = "Error: El correo electrónico no tiene un formato válido.";
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
                if (response.IsSuccess)
                {
                    NavigateToMainPage();
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

        private void NavigateToMainPage()
        {
            EmailEntry.Text = string.Empty;
            PasswordEntry.Text = string.Empty;
            Preferences.Set("IsLoggedIn", true);

            var serviciosService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ServicioService>();
            var newPage = new NavigationPage(new InicioPages(_authService, serviciosService));

            if (Application.Current?.Windows.Count > 0)
            {
                Application.Current.Windows[0].Page = newPage;
            }
        }

    }
}