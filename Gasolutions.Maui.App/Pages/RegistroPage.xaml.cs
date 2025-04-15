using Gasolutions.Maui.App.Models;
using Gasolutions.Maui.App.Services;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

            // Mostrar indicador de carga
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            ErrorLabel.IsVisible = false;

            // Deshabilitar botones durante la operación
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
                // Restablecer la UI
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
                registroButton.IsEnabled = true;
            }
        }

        private bool ValidarFormulario()
        {
            // Validar campos obligatorios
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

            // Validar que la cédula sea un número válido
            if (!long.TryParse(CedulaEntry.Text, out _))
            {
                ErrorLabel.Text = "La cédula debe ser un número válido";
                ErrorLabel.IsVisible = true;
                return false;
            }

            // Validar formato de correo electrónico
            if (!IsValidEmail(EmailEntry.Text))
            {
                ErrorLabel.Text = "El formato del correo electrónico no es válido";
                ErrorLabel.IsVisible = true;
                return false;
            }

            // Validar que las contraseñas coincidan
            if (PasswordEntry.Text != ConfirmPasswordEntry.Text)
            {
                ErrorLabel.Text = "Las contraseñas no coinciden";
                ErrorLabel.IsVisible = true;
                return false;
            }

            // Validar seguridad de la contraseña
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
            // Verificar longitud mínima
            if (password.Length < 8)
                return false;

            // Verificar que contenga al menos una letra mayúscula
            if (!password.Any(char.IsUpper))
                return false;

            // Verificar que contenga al menos una letra minúscula
            if (!password.Any(char.IsLower))
                return false;

            // Verificar que contenga al menos un número
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