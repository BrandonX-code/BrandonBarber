using System.Text;
using System.Text.Json;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class SolicitarAdminPage : ContentPage
    {
        private readonly HttpClient _httpClient;
        private bool _isNavigating = false;
        public SolicitarAdminPage()
        {
            InitializeComponent();
            _httpClient = App.Current!.Handler.MauiContext!.Services.GetService<AuthService>()!._BaseClient;
        }

        private async void OnEnviarSolicitudClicked(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
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
                LoadingIndicator.IsLoading = true;

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
                    else if ((int)response.StatusCode == 409)
                    {
                        // Leer el mensaje específico del servidor
                        var errorJson = await response.Content.ReadAsStringAsync();

                        try
                        {
                            var errorResponse = JsonSerializer.Deserialize<Dictionary<string, string>>(errorJson,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            if (errorResponse != null && errorResponse.ContainsKey("message"))
                            {
                                await AppUtils.MostrarSnackbar(errorResponse["message"], Colors.Orange, Colors.White);
                            }
                            else
                            {
                                await AppUtils.MostrarSnackbar("Ya existe un conflicto con los datos proporcionados", Colors.Orange, Colors.White);
                            }
                        }
                        catch
                        {
                            await AppUtils.MostrarSnackbar("Ya existe un conflicto con los datos proporcionados", Colors.Orange, Colors.White);
                        }
                    }
                    else
                    {
                        await DisplayAlert("Error", $"No se pudo enviar la solicitud. Código: {(int)response.StatusCode}", "OK");
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
                    this.IsEnabled = true;
                }
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
                _isNavigating = false;
            }
        }
        private async void OnVolverLoginClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
