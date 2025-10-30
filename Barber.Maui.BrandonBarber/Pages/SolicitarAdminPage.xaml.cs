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
            // Validaciones
            if (string.IsNullOrWhiteSpace(CedulaEntry.Text))
            {
                await AppUtils.MostrarSnackbar("La cédula es obligatoria", Colors.Red, Colors.White);
                return;
            }

            if (!long.TryParse(CedulaEntry.Text, out long cedula) || cedula <= 0)
            {
                await AppUtils.MostrarSnackbar("La cédula debe ser un número válido", Colors.Red, Colors.White);
                return;
            }

            if (string.IsNullOrWhiteSpace(NombreEntry.Text))
            {
                await AppUtils.MostrarSnackbar("El nombre completo es obligatorio", Colors.Red, Colors.White);
                return;
            }

            if (NombreEntry.Text.Trim().Length < 3)
            {
                await AppUtils.MostrarSnackbar("El nombre debe tener al menos 3 caracteres", Colors.Red, Colors.White);
                return;
            }

            if (string.IsNullOrWhiteSpace(EmailEntry.Text))
            {
                await AppUtils.MostrarSnackbar("El email es obligatorio", Colors.Red, Colors.White);
                return;
            }

            if (!EmailEntry.Text.Contains("@") || !EmailEntry.Text.Contains("."))
            {
                await AppUtils.MostrarSnackbar("El email no es válido", Colors.Red, Colors.White);
                return;
            }

            if (string.IsNullOrWhiteSpace(JustificacionEditor.Text))
            {
                await AppUtils.MostrarSnackbar("La justificación es obligatoria", Colors.Red, Colors.White);
                return;
            }

            if (JustificacionEditor.Text.Trim().Length < 20)
            {
                await AppUtils.MostrarSnackbar("La justificación debe tener al menos 20 caracteres", Colors.Red, Colors.White);
                return;
            }

            if (!string.IsNullOrWhiteSpace(TelefonoEntry.Text) && TelefonoEntry.Text.Length < 7)
            {
                await AppUtils.MostrarSnackbar("El teléfono no es válido", Colors.Red, Colors.White);
                return;
            }

            this.IsEnabled = false;
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
                    await AppUtils.MostrarSnackbar("Solicitud enviada. Recibirás un email cuando sea revisada", Colors.Green, Colors.White);
                    await Navigation.PopAsync();
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
                this.IsEnabled = true;
            }
        }
    }
}
