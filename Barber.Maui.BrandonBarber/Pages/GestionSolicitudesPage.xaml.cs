using System.Text;
using System.Text.Json;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class GestionSolicitudesPage : ContentPage
    {
        private readonly HttpClient _httpClient;

        public GestionSolicitudesPage()
        {
            InitializeComponent();
            _httpClient = App.Current!.Handler.MauiContext!.Services.GetService<AuthService>()!._BaseClient;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarSolicitudes();
        }

        private async Task CargarSolicitudes()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/solicitudes/pendientes");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var solicitudes = JsonSerializer.Deserialize<List<SolicitudAdministrador>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    SolicitudesCollection.ItemsSource = solicitudes;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void OnAprobarClicked(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var solicitud = (SolicitudAdministrador)button.CommandParameter;

            bool confirm = await DisplayAlert("Confirmar",
                $"¿Aprobar solicitud de {solicitud.NombreSolicitante}?", "Sí", "No");

            if (!confirm) return;

            try
            {
                var data = new { cedulaRevisor = AuthService.CurrentUser!.Cedula };
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"api/solicitudes/{solicitud.Id}/aprobar", content);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Éxito", "Solicitud aprobada", "OK");
                    await CargarSolicitudes();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void OnRechazarClicked(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var solicitud = (SolicitudAdministrador)button.CommandParameter;

            string motivo = await DisplayPromptAsync("Rechazar",
                "Motivo del rechazo:", "Enviar", "Cancelar");

            if (string.IsNullOrWhiteSpace(motivo)) return;

            try
            {
                var data = new
                {
                    cedulaRevisor = AuthService.CurrentUser!.Cedula,
                    motivoRechazo = motivo
                };
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"api/solicitudes/{solicitud.Id}/rechazar", content);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Éxito", "Solicitud rechazada", "OK");
                    await CargarSolicitudes();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}