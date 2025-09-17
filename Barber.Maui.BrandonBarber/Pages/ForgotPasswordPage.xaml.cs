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

        private async void OnSendCodeClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EmailEntry.Text))
            {
                ShowError("Por favor, ingresa tu email");
                return;
            }

            // Validar formato del email
            var emailValidator = new EmailAddressAttribute();
            if (!emailValidator.IsValid(EmailEntry.Text))
            {
                ShowError("Por favor, ingresa un email válido");
                return;
            }

            SetLoadingState(true);
            HideMessages();

            try
            {
                var response = await _authService.ForgotPassword(EmailEntry.Text.Trim());

                if (response.IsSuccess)
                {
                    _currentEmail = EmailEntry.Text.Trim();
                    ShowMessage(response.Message, isError: false);

                    // Cambiar a la segunda pantalla
                    EmailStep.IsVisible = false;
                    ResetStep.IsVisible = true;

                    // Focus en el campo de token
                    await Task.Delay(300);
                    TokenEntry.Focus();
                }
                else
                {
                    ShowError(response.Message);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error: {ex.Message}");
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async void OnResetPasswordClicked(object sender, EventArgs e)
        {
            if (!ValidateResetFields())
                return;

            SetLoadingState(true);
            HideMessages();

            try
            {
                var response = await _authService.ResetPassword(
                    _currentEmail,
                    TokenEntry.Text.Trim(),
                    NewPasswordEntry.Text.Trim()
                );

                if (response.IsSuccess)
                {
                    ShowMessage("¡Contraseña restablecida exitosamente! Redirigiendo...", isError: false);

                    // Esperar un momento y volver al login
                    await Task.Delay(2000);
                    await NavigateToLogin();
                }
                else
                {
                    ShowError(response.Message);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error: {ex.Message}");
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async void OnResendCodeClicked(object sender, EventArgs e)
        {
            SetLoadingState(true);
            HideMessages();

            try
            {
                var response = await _authService.ForgotPassword(_currentEmail);

                if (response.IsSuccess)
                {
                    ShowMessage("Código reenviado exitosamente", isError: false);
                    TokenEntry.Text = string.Empty;
                    TokenEntry.Focus();
                }
                else
                {
                    ShowError(response.Message);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error: {ex.Message}");
            }
            finally
            {
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
                ShowError("Por favor, ingresa el código recibido");
                return false;
            }

            if (TokenEntry.Text.Trim().Length != 6)
            {
                ShowError("El código debe tener 6 dígitos");
                return false;
            }

            if (string.IsNullOrWhiteSpace(NewPasswordEntry.Text))
            {
                ShowError("Por favor, ingresa tu nueva contraseña");
                return false;
            }

            if (NewPasswordEntry.Text.Length < 6)
            {
                ShowError("La contraseña debe tener al menos 6 caracteres");
                return false;
            }

            if (string.IsNullOrWhiteSpace(ConfirmPasswordEntry.Text))
            {
                ShowError("Por favor, confirma tu nueva contraseña");
                return false;
            }

            if (NewPasswordEntry.Text != ConfirmPasswordEntry.Text)
            {
                ShowError("Las contraseñas no coinciden");
                return false;
            }

            return true;
        }

        private void ShowMessage(string message, bool isError)
        {
            if (isError)
            {
                ErrorLabel.Text = message;
                ErrorLabel.IsVisible = true;
                MessageLabel.IsVisible = false;
            }
            else
            {
                MessageLabel.Text = message;
                MessageLabel.IsVisible = true;
                ErrorLabel.IsVisible = false;
            }
        }

        private void ShowError(string message)
        {
            ShowMessage(message, isError: true);
        }

        private void HideMessages()
        {
            ErrorLabel.IsVisible = false;
            MessageLabel.IsVisible = false;
        }

        private void SetLoadingState(bool isLoading)
        {
            LoadingIndicator.IsVisible = isLoading;
            LoadingIndicator.IsRunning = isLoading;
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
                HideMessages();
                return true; // Consumir el evento
            }

            // Si estamos en el paso 1, comportamiento normal
            return base.OnBackButtonPressed();
        }
    }
}