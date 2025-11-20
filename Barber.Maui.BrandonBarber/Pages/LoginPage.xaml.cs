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

        protected override void OnAppearing()
        {
            base.OnAppearing();
            EmailEntry.Text = string.Empty;
            PasswordEntry.Text = string.Empty;
            Console.WriteLine("🔷 LoginPage - Mostrando formulario de login");
        }
        private void OnTogglePasswordClicked(object sender, EventArgs e)
        {
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;

            TogglePasswordIcon.Source = PasswordEntry.IsPassword
                ? "ojocerrado.png"
                : "ojoabierto.png";
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EmailEntry.Text) && string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                await AppUtils.MostrarSnackbar("Por favor, completa todos los campos", Colors.Red, Colors.White);
                return;
            }

            if (string.IsNullOrWhiteSpace(EmailEntry.Text))
            {
                await AppUtils.MostrarSnackbar("Por favor, ingrese el correo", Colors.Red, Colors.White);
                return;
            }

            if (string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                await AppUtils.MostrarSnackbar("Por favor, ingrese la contraseña", Colors.Red, Colors.White);
                return;
            }


            var emailValidator = new EmailAddressAttribute();
            if (!emailValidator.IsValid(EmailEntry.Text))
            {
                await AppUtils.MostrarSnackbar("El correo electrónico no tiene un formato válido.", Colors.Red, Colors.White);
                return;
            }

            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsLoading = true;

            var loginButton = (Button)sender;
            loginButton.IsEnabled = false;

            try
            {
                var response = await _authService.Login(EmailEntry.Text, PasswordEntry.Text);
                if (response.IsSuccess)
                {
                    await AppUtils.MostrarSnackbar("Inicio de sesión exitoso", Colors.Green, Colors.White);
                    NavigateToMainPage();
                }
                else
                {
                    await AppUtils.MostrarSnackbar(response.Message!, Colors.Red, Colors.White);
                }
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error: {ex.Message}", Colors.Red, Colors.White);
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
                _ = AppUtils.MostrarSnackbar("Error al navegar. Intenta nuevamente.", Colors.Red, Colors.White);
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
