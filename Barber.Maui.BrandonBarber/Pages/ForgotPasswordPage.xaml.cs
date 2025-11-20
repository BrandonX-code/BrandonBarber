using System.ComponentModel.DataAnnotations;
using Barber.Maui.BrandonBarber.Services;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class ForgotPasswordPage : ContentPage
    {
        private readonly AuthService _authService;
        private string _currentEmail = string.Empty;

        public ForgotPasswordPage()
        {
            InitializeComponent();
            _authService = Application.Current!.Handler.MauiContext!.Services.GetService<AuthService>()!;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            EmailEntry.Text = string.Empty;
        }
        private async void OnSendCodeClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EmailEntry.Text))
            {
                await AppUtils.MostrarSnackbar("Por favor, ingresa tu email", Colors.Red, Colors.White);
                return;
            }

            // Validar formato del email
            var emailValidator = new EmailAddressAttribute();
            if (!emailValidator.IsValid(EmailEntry.Text))
            {
                await AppUtils.MostrarSnackbar("Por favor, ingresa un email válido", Colors.Red , Colors.White);
                return;
            }
            ((Button)sender).IsEnabled = false;
            this.IsEnabled = false;
            SetLoadingState(true);

            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                var response = await _authService.ForgotPassword(EmailEntry.Text.Trim());

                if (response.IsSuccess)
                {
                    _currentEmail = EmailEntry.Text.Trim();
                    await AppUtils.MostrarSnackbar(response.Message!, Colors.Green, Colors.White); // Verde para éxito
                                                                                                   // Cambiar a la segunda pantalla
                    EmailStep.IsVisible = false;
                    ResetStep.IsVisible = true;

                    // Focus en el campo de token
                    await Task.Delay(300);
                    TokenEntry.Focus();
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
                ((Button)sender).IsEnabled = true;
                this.IsEnabled = true;
                SetLoadingState(false);
            }
        }

        private async void OnResetPasswordClicked(object sender, EventArgs e)
        {
            if (!ValidateResetFields())
                return;

            SetLoadingState(true);
            ((Button)sender).IsEnabled = false;
            this.IsEnabled = false;
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                var response = await _authService.ResetPassword(
                    _currentEmail,
                    TokenEntry.Text.Trim(),
                    NewPasswordEntry.Text.Trim()
                );

                if (response.IsSuccess)
                {
                    await AppUtils.MostrarSnackbar("¡Contraseña restablecida exitosamente! Redirigiendo...", Colors.Red, Colors.White);

                    // Esperar un momento y volver al login
                    await Task.Delay(2000);
                    await NavigateToLogin();
                }
                else
                {
                    await AppUtils.MostrarSnackbar(response.Message, Colors.Red, Colors.White);
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
                ((Button)sender).IsEnabled = true;
                this.IsEnabled = true;
                SetLoadingState(false);
            }
        }

        private async void OnResendCodeClicked(object sender, EventArgs e)
        {
            SetLoadingState(true);
            ((Button)sender).IsEnabled = false;
            this.IsEnabled = false;
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                var response = await _authService.ForgotPassword(_currentEmail);

                if (response.IsSuccess)
                {
                    await AppUtils.MostrarSnackbar("Código reenviado exitosamente", Colors.Red, Colors.White);
                    TokenEntry.Text = string.Empty;
                    TokenEntry.Focus();
                }
                else
                {
                    await AppUtils.MostrarSnackbar(response.Message, Colors.Red, Colors.White);
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
                ((Button)sender).IsEnabled = true;
                this.IsEnabled = true;
                SetLoadingState(false);
            }
        }

        private async void OnBackToLoginClicked(object sender, EventArgs e)
        {
            await NavigateToLogin();
        }

        private bool ValidateResetFields()
        {
            if (string.IsNullOrWhiteSpace(TokenEntry.Text))
            {
                _ = AppUtils.MostrarSnackbar("Por favor, ingresa el código recibido", Colors.Red, Colors.White);
                return false;
            }

            if (TokenEntry.Text.Trim().Length != 6)
            {
                _ = AppUtils.MostrarSnackbar("El código debe tener 6 dígitos", Colors.Red, Colors.White);
                return false;
            }

            if (string.IsNullOrWhiteSpace(NewPasswordEntry.Text))
            {
                _ = AppUtils.MostrarSnackbar("Por favor, ingresa tu nueva contraseña", Colors.Red, Colors.White);
                return false;
            }

            if (NewPasswordEntry.Text.Length < 6)
            {
                _ = AppUtils.MostrarSnackbar("La contraseña debe tener al menos 6 caracteres", Colors.Red, Colors.White);
                return false;
            }

            if (string.IsNullOrWhiteSpace(ConfirmPasswordEntry.Text))
            {
                _ = AppUtils.MostrarSnackbar("Por favor, confirma tu nueva contraseña", Colors.Red, Colors.White);
                return false;
            }

            if (NewPasswordEntry.Text != ConfirmPasswordEntry.Text)
            {
                _ = AppUtils.MostrarSnackbar("Las contraseñas no coinciden", Colors.Red, Colors.White);
                return false;
            }

            return true;
        }

        private void SetLoadingState(bool isLoading)
        {
            LoadingIndicator.IsVisible = isLoading;
        }

        private async Task NavigateToLogin()
        {
            await Navigation.PopAsync();
        }

        protected override bool OnBackButtonPressed()
        {
            // Si estamos en el paso 2, volver al paso 1
            if (ResetStep.IsVisible)
            {
                ResetStep.IsVisible = false;
                EmailStep.IsVisible = true;
                return true; // Consumir el evento
            }

            // Si estamos en el paso 1, comportamiento normal
            return base.OnBackButtonPressed();
        }
    }
}