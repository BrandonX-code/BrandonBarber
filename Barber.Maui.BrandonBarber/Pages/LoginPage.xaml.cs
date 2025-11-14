using System.ComponentModel.DataAnnotations;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class LoginPage : ContentPage
    {
        private readonly AuthService _authService;
        private bool _isNavigating = false;

        public LoginPage()
        {
            InitializeComponent();
            _authService = Application.Current!.Handler.MauiContext!.Services.GetService<AuthService>()!;
        }

        // 🔥 YA NO necesitas verificar sesión en OnAppearing
        // El SplashPage ya lo hizo
        protected override void OnAppearing()
        {
            base.OnAppearing();
            Console.WriteLine("🔷 LoginPage - Mostrando formulario de login");
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EmailEntry.Text) || string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                ErrorLabel.Text = "Por favor, completa todos los campos";
                ErrorLabel.IsVisible = true;
                return;
            }

            var emailValidator = new EmailAddressAttribute();
            if (!emailValidator.IsValid(EmailEntry.Text))
            {
                ErrorLabel.Text = "Error: El correo electrónico no tiene un formato válido.";
                ErrorLabel.IsVisible = true;
                return;
            }

            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsLoading = true;
            ErrorLabel.IsVisible = false;

            var loginButton = (Button)sender;
            loginButton.IsEnabled = false;

            try
            {
                var response = await _authService.Login(EmailEntry.Text, PasswordEntry.Text);
                if (response.IsSuccess)
                {
                    Console.WriteLine("🔷 Login exitoso - Navegando a MainPage");
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
                LoadingIndicator.IsLoading = false;
                loginButton.IsEnabled = true;
            }
        }

        private async void OnRegistroClicked(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                await Navigation.PushAsync(new RegistroPage());
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private void NavigateToMainPage()
        {
            if (_isNavigating) return;
            _isNavigating = true;

            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en NavigateToMainPage: {ex.Message}");
                ErrorLabel.Text = "Error al navegar. Intenta nuevamente.";
                ErrorLabel.IsVisible = true;
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private async void OnForgotPasswordClicked(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                await Navigation.PushAsync(new ForgotPasswordPage());
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private async void OnSolicitarAdminClicked(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                await Navigation.PushAsync(new SolicitarAdminPage());
            }
            finally
            {
                _isNavigating = false;
            }
        }
    }
}