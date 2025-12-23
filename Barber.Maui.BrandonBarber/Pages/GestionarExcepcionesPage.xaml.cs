using System.Text.Json;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class GestionarExcepcionesPage : ContentPage
    {
        private readonly HttpClient _httpClient;
        private readonly ReservationService _reservationService;
        private List<DisponibilidadExcepcionalModel> _excepciones = new();

        public GestionarExcepcionesPage()
        {
            InitializeComponent();
            _httpClient = App.Current!.Handler.MauiContext!.Services.GetService<HttpClient>()!;
            _reservationService = App.Current!.Handler.MauiContext!.Services.GetService<ReservationService>()!;

            FechaPicker.Date = DateTime.Today.AddDays(1); // Por defecto mañana
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarExcepciones();
        }

        private async Task CargarExcepciones()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;

                var barbero = AuthService.CurrentUser;
                if (barbero == null) return;

                var response = await _httpClient.GetAsync($"api/disponibilidad-excepcional/barbero/{barbero.Cedula}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _excepciones = JsonSerializer.Deserialize<List<DisponibilidadExcepcionalModel>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

                    // Filtrar solo excepciones futuras
                    _excepciones = _excepciones.Where(e => e.Fecha.Date >= DateTime.Today).ToList();

                    ExcepcionesCollectionView.ItemsSource = _excepciones;
                }
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"No se pudieron cargar las excepciones: {ex.Message}", Colors.Red, Colors.White);
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }

        private void OnTipoExcepcionChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is RadioButton radio && radio.IsChecked)
            {
                HorariosModificadosContainer.IsVisible = radio == HorarioModificadoRadio;
            }
        }

        private async void OnFechaSelected(object sender, DateChangedEventArgs e)
        {
            await ActualizarCitasAfectadas();
        }

        private async Task ActualizarCitasAfectadas()
        {
            try
            {
                var barbero = AuthService.CurrentUser;
                if (barbero == null) return;

                var fecha = FechaPicker.Date;
                var citas = await _reservationService.GetReservationsByBarberoAndFecha(barbero.Cedula, fecha);

                // Filtrar solo citas activas
                var citasActivas = citas.Where(c =>
                    c.Estado != "Cancelada" &&
                    c.Estado != "Finalizada").ToList();

                if (citasActivas.Any())
                {
                    CitasAfectadasContainer.IsVisible = true;
                    CitasAfectadasLabel.Text = $"⚠️ Citas afectadas: {citasActivas.Count}";

                    var nombres = string.Join(", ", citasActivas.Select(c => c.Nombre));
                    CitasDetalleLabel.Text = $"Clientes: {nombres}\n\nSe les notificará automáticamente.";
                }
                else
                {
                    CitasAfectadasContainer.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al actualizar citas afectadas: {ex.Message}");
            }
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            try
            {
                var barbero = AuthService.CurrentUser;
                if (barbero == null)
                {
                    await AppUtils.MostrarSnackbar("No se pudo identificar al barbero", Colors.Red, Colors.White);
                    return;
                }

                // Validación
                if (FechaPicker.Date < DateTime.Today)
                {
                    await AppUtils.MostrarSnackbar("No puedes crear excepciones para fechas pasadas", Colors.Red, Colors.White);
                    return;
                }

                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;

                var excepcion = new DisponibilidadExcepcionalModel
                {
                    BarberoId = barbero.Cedula,
                    Fecha = FechaPicker.Date,
                    DiaCompleto = DiaCompletoRadio.IsChecked,
                    TipoExcepcion = DiaCompletoRadio.IsChecked ? "DiaCompleto" : "HorarioModificado",
                    Motivo = string.IsNullOrWhiteSpace(MotivoEditor.Text) ? null : MotivoEditor.Text
                };

                // Si es horario modificado, generar horarios
                if (HorarioModificadoRadio.IsChecked)
                {
                    if (HoraFinPicker.Time <= HoraInicioPicker.Time)
                    {
                        await AppUtils.MostrarSnackbar("La hora de fin debe ser mayor que la hora de inicio", Colors.Red, Colors.White);
                        return;
                    }

                    var horarios = GenerarHorariosModificados(HoraInicioPicker.Time, HoraFinPicker.Time);
                    excepcion.HorariosModificados = JsonSerializer.Serialize(horarios);
                }

                var json = JsonSerializer.Serialize(excepcion);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/disponibilidad-excepcional", content);

                if (response.IsSuccessStatusCode)
                {
                    await AppUtils.MostrarSnackbar("✅ Excepción creada y clientes notificados", Colors.Green, Colors.White);

                    // Limpiar formulario
                    FechaPicker.Date = DateTime.Today.AddDays(1);
                    MotivoEditor.Text = string.Empty;
                    DiaCompletoRadio.IsChecked = true;
                    CitasAfectadasContainer.IsVisible = false;

                    await CargarExcepciones();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await AppUtils.MostrarSnackbar($"No se pudo crear la excepción: {error}", Colors.Red, Colors.White);
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
            }
        }

        private Dictionary<string, bool> GenerarHorariosModificados(TimeSpan inicio, TimeSpan fin)
        {
            var horarios = new Dictionary<string, bool>();
            var duracion = TimeSpan.FromMinutes(40);
            var horaActual = inicio;

            while (horaActual + duracion <= fin)
            {
                var siguienteHora = horaActual + duracion;
                var formato = $"{FormatearHora(horaActual)} - {FormatearHora(siguienteHora)}";
                horarios[formato] = true;
                horaActual = siguienteHora;
            }

            return horarios;
        }

        private string FormatearHora(TimeSpan hora)
        {
            var dt = DateTime.Today.Add(hora);
            return dt.ToString("hh:mm tt");
        }

        private async void OnEliminarExcepcionClicked(object sender, EventArgs e)
        {
            try
            {
                if (sender is Button button && button.CommandParameter is DisponibilidadExcepcionalModel excepcion)
                {
                    var popup = new CustomAlertPopup($"¿Eliminar la excepción del {excepcion.Fecha:dd/MM/yyyy}?");
                    bool confirm = await popup.ShowAsync(this);
                    if (!confirm) return;

                    LoadingIndicator.IsVisible = true;
                    LoadingIndicator.IsLoading = true;

                    var response = await _httpClient.DeleteAsync($"api/disponibilidad-excepcional/{excepcion.Id}");

                    if (response.IsSuccessStatusCode)
                    {
                        await AppUtils.MostrarSnackbar("Excepción eliminada", Colors.Orange, Colors.White);
                        await CargarExcepciones();
                    }
                    else
                    {
                        await AppUtils.MostrarSnackbar("No se pudo eliminar la excepción", Colors.Red, Colors.White);
                    }
                }
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar(ex.Message, Colors.Red, Colors.White);
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }
    }

    // Modelo para el cliente
    public class DisponibilidadExcepcionalModel
    {
        public int Id { get; set; }
        public long BarberoId { get; set; }
        public DateTime Fecha { get; set; }
        public string TipoExcepcion { get; set; } = string.Empty;
        public string? Motivo { get; set; }
        public string? HorariosModificados { get; set; }
        public bool DiaCompleto { get; set; }
        public DateTime FechaCreacion { get; set; }
        public bool ClientesNotificados { get; set; }
        public string? CitasAfectadas { get; set; }
    }
}