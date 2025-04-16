using Gasolutions.Maui.App.Models;
using Gasolutions.Maui.App.Services;
using System.Text.RegularExpressions;

namespace Gasolutions.Maui.App.Pages
{
    public partial class RegistroPage : ContentPage
    {
        private readonly AuthService _authService;

        public RegistroPage()
        {
            InitializeComponent();
            _authService = Application.Current.Handler.MauiContext.Services.GetService<AuthService>();
        }

        private async void OnRegistrarClicked(object sender, EventArgs e)
        {
            if (!ValidarFormulario())
            {
                return;
            }

            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            ErrorLabel.IsVisible = false;

            var registroButton = (Button)sender;
            registroButton.IsEnabled = false;

            try
            {
                var registroRequest = new RegistroRequest
                {
                    Nombre = NombreEntry.Text,
                    Cedula = long.Parse(CedulaEntry.Text),
                    Email = EmailEntry.Text,
                    Contraseña = PasswordEntry.Text,
                    ConfirmContraseña = ConfirmPasswordEntry.Text,
                    Telefono = TelefonoEntry.Text,
                    Direccion = DireccionEntry.Text,
                    Rol = "user"
                };

                var response = await _authService.Register(registroRequest);
                Console.WriteLine($"Respuesta de registro: Success = {response.Success}, Message = {response.Message}");

                if (response.Success)
                {
                    await DisplayAlert("Registro Exitoso", "Tu cuenta ha sido creada correctamente. Ahora puedes iniciar sesión.", "OK");
                    await Navigation.PopAsync();
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
                registroButton.IsEnabled = true;
            }
        }

        private bool ValidarFormulario()
        {
            if (string.IsNullOrWhiteSpace(NombreEntry.Text) ||
                string.IsNullOrWhiteSpace(CedulaEntry.Text) ||
                string.IsNullOrWhiteSpace(EmailEntry.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry.Text) ||
                string.IsNullOrWhiteSpace(ConfirmPasswordEntry.Text))
            {
                ErrorLabel.Text = "Por favor, completa todos los campos obligatorios";
                ErrorLabel.IsVisible = true;
                return false;
            }

            if (!long.TryParse(CedulaEntry.Text, out _))
            {
                ErrorLabel.Text = "La cédula debe ser un número válido";
                ErrorLabel.IsVisible = true;
                return false;
            }

            if (!IsValidEmail(EmailEntry.Text))
            {
                ErrorLabel.Text = "El formato del correo electrónico no es válido";
                ErrorLabel.IsVisible = true;
                return false;
            }

            if (PasswordEntry.Text != ConfirmPasswordEntry.Text)
            {
                ErrorLabel.Text = "Las contraseñas no coinciden";
                ErrorLabel.IsVisible = true;
                return false;
            }

            if (!IsPasswordSecure(PasswordEntry.Text))
            {
                ErrorLabel.Text = "La contraseña debe tener al menos 8 caracteres, incluir letras mayúsculas, minúsculas y números";
                ErrorLabel.IsVisible = true;
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return regex.IsMatch(email);
        }

        private bool IsPasswordSecure(string password)
        {
            if (password.Length < 8)
                return false;

            if (!password.Any(char.IsUpper))
                return false;

            if (!password.Any(char.IsLower))
                return false;

            if (!password.Any(char.IsDigit))
                return false;

            return true;
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}