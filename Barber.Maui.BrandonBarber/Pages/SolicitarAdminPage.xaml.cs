using System.Text;
using System.Text.Json;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class SolicitarAdminPage : ContentPage
    {
        private readonly HttpClient _httpClient;

        public SolicitarAdminPage()
        {
            InitializeComponent();
            _httpClient = App.Current!.Handler.MauiContext!.Services.GetService<AuthService>()!._BaseClient;
        }

        private async void OnEnviarSolicitudClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CedulaEntry.Text) ||
                string.IsNullOrWhiteSpace(NombreEntry.Text) ||
                string.IsNullOrWhiteSpace(EmailEntry.Text) ||
                string.IsNullOrWhiteSpace(JustificacionEditor.Text))
            {
                await DisplayAlert("Error", "Todos los campos son obligatorios", "OK");
                return;
            }

            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            try
            {
                var solicitud = new SolicitudAdministrador
                {
                    CedulaSolicitante = long.Parse(CedulaEntry.Text),
                    NombreSolicitante = NombreEntry.Text,
                    EmailSolicitante = EmailEntry.Text,
                    TelefonoSolicitante = TelefonoEntry.Text,
                    Justificacion = JustificacionEditor.Text
                };

                var json = JsonSerializer.Serialize(solicitud);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/solicitudes/crear", content);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Éxito", "Solicitud enviada. Recibirás un email cuando sea revisada", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    MessageLabel.Text = "Error al enviar solicitud";
                    MessageLabel.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }
    }
}
