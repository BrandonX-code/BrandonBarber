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
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                var response = await _httpClient.GetAsync("api/solicitudes/pendientes");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var solicitudes = JsonSerializer.Deserialize<List<SolicitudAdministrador>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    SolicitudesCollection.ItemsSource = solicitudes;

                    // Mostrar/ocultar mensaje de lista vacía
                    if (solicitudes == null || solicitudes.Count == 0)
                    {
                        SolicitudesCollection.IsVisible = false;
                        EmptyStateLayout.IsVisible = true;
                    }
                    else
                    {
                        SolicitudesCollection.IsVisible = true;
                        EmptyStateLayout.IsVisible = false;
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }

        private async void OnAprobarClicked(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var solicitud = (SolicitudAdministrador)button.CommandParameter;

            var popup = new CustomAlertPopup($"¿Aprobar solicitud de {solicitud.NombreSolicitante}?");
            bool confirmacion = await popup.ShowAsync(this);

            if (!confirmacion) return;

            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                var data = new { cedulaRevisor = AuthService.CurrentUser!.Cedula };
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"api/solicitudes/{solicitud.Id}/aprobar", content);

                if (response.IsSuccessStatusCode)
                {
                    await AppUtils.MostrarSnackbar("Solicitud aprobada", Colors.Green, Colors.White);
                    await CargarSolicitudes();
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo aprobar la solicitud", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
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
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
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
                    await AppUtils.MostrarSnackbar("Solicitud rechazada", Colors.Green, Colors.White);
                    await CargarSolicitudes();
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo rechazar la solicitud", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }
    }
}