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

        private async void OnTipoExcepcionChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is RadioButton radio && radio.IsChecked)
            {
                HorariosModificadosContainer.IsVisible = radio == HorarioModificadoRadio;

                // ✅ NUEVO: Si selecciona "Horarios diferentes", cargar disponibilidad del barbero
                if (radio == HorarioModificadoRadio)
                {
                    await CargarHorariosDisponibilidad();
                }
            }
        }
        // ✅ NUEVO MÉTODO: Cargar horarios de la disponibilidad normal del barbero
        private async Task CargarHorariosDisponibilidad()
        {
            try
            {
                var barbero = AuthService.CurrentUser;
                if (barbero == null) return;

                var disponibilidadService = App.Current!.Handler.MauiContext!.Services
                    .GetService<DisponibilidadService>();

                if (disponibilidadService == null) return;

                // Obtener disponibilidad para la fecha seleccionada
                var disponibilidad = await disponibilidadService.GetDisponibilidad(
                    FechaPicker.Date, barbero.Cedula);

                if (disponibilidad == null || !disponibilidad.HorariosDict.Any(h => h.Value))
                {
                    // Si no hay disponibilidad, usar horarios por defecto
                    HoraInicioPicker.Time = new TimeSpan(9, 0, 0);  // 9:00 AM
                    HoraFinPicker.Time = new TimeSpan(18, 0, 0);    // 6:00 PM
                    return;
                }

                // ✅ Extraer primera y última hora de los horarios disponibles
                var horariosDisponibles = disponibilidad.HorariosDict
                    .Where(h => h.Value)
                    .Select(h => h.Key)
                    .ToList();

                if (horariosDisponibles.Any())
                {
                    // Tomar el primer horario (ejemplo: "07:00 AM - 07:40 AM")
                    var primerHorario = horariosDisponibles.First();
                    var ultimoHorario = horariosDisponibles.Last();

                    // Extraer hora de inicio del primer horario
                    var horaInicio = ParsearHora(primerHorario.Split('-')[0].Trim());

                    // Extraer hora de fin del último horario
                    var horaFin = ParsearHora(ultimoHorario.Split('-')[1].Trim());

                    HoraInicioPicker.Time = horaInicio;
                    HoraFinPicker.Time = horaFin;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cargando horarios: {ex.Message}");
                // Valores por defecto en caso de error
                HoraInicioPicker.Time = new TimeSpan(9, 0, 0);
                HoraFinPicker.Time = new TimeSpan(18, 0, 0);
            }
        }

        // ✅ NUEVO MÉTODO: Convertir texto de hora a TimeSpan
        private TimeSpan ParsearHora(string horaTexto)
        {
            try
            {
                // Normalizar el texto (ej: "07:00 AM", "7:00 a.m.", etc)
                horaTexto = horaTexto
                    .Replace("a.m.", "AM", StringComparison.OrdinalIgnoreCase)
                    .Replace("p.m.", "PM", StringComparison.OrdinalIgnoreCase)
                    .Replace("a. m.", "AM", StringComparison.OrdinalIgnoreCase)
                    .Replace("p. m.", "PM", StringComparison.OrdinalIgnoreCase)
                    .Trim();

                if (DateTime.TryParse(horaTexto, out DateTime resultado))
                {
                    return resultado.TimeOfDay;
                }

                // Si falla, retornar 9:00 AM por defecto
                return new TimeSpan(9, 0, 0);
            }
            catch
            {
                return new TimeSpan(9, 0, 0);
            }
        }

        private async void OnFechaSelected(object sender, DateChangedEventArgs e)
        {
            await ActualizarCitasAfectadas();

            // ✅ NUEVO: Si ya tiene seleccionado "Horarios diferentes", recargar horarios
            if (HorarioModificadoRadio.IsChecked)
            {
                await CargarHorariosDisponibilidad();
            }
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
            var duracion = TimeSpan.FromMinutes(60);
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