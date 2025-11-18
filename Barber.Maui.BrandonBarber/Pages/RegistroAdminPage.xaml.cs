using System.Text.Json;
using System.Net.Http;
using System.Text;
using Barber.Maui.BrandonBarber.Models;
using Microsoft.Maui.Controls;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class RegistroAdminPage : ContentPage, IQueryAttributable
    {
        private int _solicitudId;
        private readonly HttpClient _httpClient;
        public RegistroAdminPage()
        {
            InitializeComponent();
            _httpClient = App.Current!.Handler.MauiContext!.Services.GetService<AuthService>()!._BaseClient;
        }
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("solicitudId", out var id))
            {
                int.TryParse(id?.ToString(), out _solicitudId);
            }
        }
        private async void OnRegistrarClicked(object sender, EventArgs e)
        {
            ErrorLabel.IsVisible = false;
            if (!ValidarFormulario()) return;
            var dto = new
            {
                SolicitudId = _solicitudId,
                Nombre = NombreEntry.Text,
                Email = EmailEntry.Text,
                Telefono = TelefonoEntry.Text,
                Direccion = DireccionEntry.Text,
                Contraseña = PasswordEntry.Text
            };
            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                var response = await _httpClient.PostAsync("api/auth/register-admin", content);
                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Éxito", "Registro de administrador exitoso.", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    ErrorLabel.Text = msg;
                    ErrorLabel.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = ex.Message;
                ErrorLabel.IsVisible = true;
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }
        private bool ValidarFormulario()
        {
            if (string.IsNullOrWhiteSpace(NombreEntry.Text) ||
            string.IsNullOrWhiteSpace(EmailEntry.Text) ||
            string.IsNullOrWhiteSpace(PasswordEntry.Text) ||
            string.IsNullOrWhiteSpace(ConfirmPasswordEntry.Text))
            {
                ErrorLabel.Text = "Completa todos los campos obligatorios.";
                ErrorLabel.IsVisible = true;
                return false;
            }
            if (PasswordEntry.Text != ConfirmPasswordEntry.Text)
            {
                ErrorLabel.Text = "Las contraseñas no coinciden.";
                ErrorLabel.IsVisible = true;
                return false;
            }
            if (!AppUtils.IsValidEmail(EmailEntry.Text))
            {
                ErrorLabel.Text = "Correo electrónico inválido.";
                ErrorLabel.IsVisible = true;
                return false;
            }
            if (!AppUtils.IsPasswordSecure(PasswordEntry.Text))
            {
                ErrorLabel.Text = "La contraseña debe tener al menos8 caracteres, incluir mayúsculas, minúsculas y números.";
                ErrorLabel.IsVisible = true;
                return false;
            }
            return true;
        }
    }
}
