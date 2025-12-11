namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class BuscarPage : ContentPage, INotifyPropertyChanged
    {
        public ObservableCollection<CitaModel> ProximasCitas { get; set; } = [];
        public ObservableCollection<CitaModel> HistorialCitas { get; set; } = [];
        private static SwipeView? _lastOpenedSwipeView;
        private bool _hasProximasCitas;
        public bool HasProximasCitas
        {
            get => _hasProximasCitas;
            set
            {
                if (_hasProximasCitas != value)
                {
                    _hasProximasCitas = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _hasHistorialCitas;
        public bool HasHistorialCitas
        {
            get => _hasHistorialCitas;
            set
            {
                if (_hasHistorialCitas != value)
                {
                    _hasHistorialCitas = value;
                    OnPropertyChanged();
                }
            }
        }

        private readonly ReservationService _reservationService;

        public BuscarPage(ReservationService reservationService)
        {
            InitializeComponent();
            BindingContext = this;
            _reservationService = reservationService;
        }
        private void OnSwipeStarted(object sender, SwipeStartedEventArgs e)
        {
            if (_lastOpenedSwipeView != null && _lastOpenedSwipeView != sender)
            {
                _lastOpenedSwipeView.Close();
            }

            _lastOpenedSwipeView = sender as SwipeView;
        }
        private void OnPendientesClicked(object sender, EventArgs e) => FiltrarPorEstado("Pendiente");
        private void OnCompletadasClicked(object sender, EventArgs e) => FiltrarPorEstado("Completada");
        private void OnCanceladasClicked(object sender, EventArgs e) => FiltrarPorEstado("Cancelada");
        private void OnFinalizadasClicked(object sender, EventArgs e) => FiltrarPorEstado("Finalizada");

        private List<CitaModel> _todasLasCitas = new();
        private string _estadoActual = "Pendiente";

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            UpdateVisibility();
            _ = ActualizarLista();
            await ActualizarListaEstados();
        }

        private async void OnSearchClicked(object sender, EventArgs e)
        {
            await ActualizarLista();
        }

        private async Task ActualizarLista()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;

                var clienteCedula = AuthService.CurrentUser!.Cedula;
                var citas = await _reservationService.GetReservationsById(clienteCedula);

                if (citas == null || citas.Count == 0)
                {
                    await AppUtils.MostrarSnackbar("No tienes citas agendadas.", Colors.Red, Colors.White);
                    UpdateVisibility();
                    return;
                }

                DateTime now = DateTime.Now.Date;

                foreach (var cita in citas)
                {
                    if (cita.Fecha.Date >= now)
                    {
                        ProximasCitas.Add(cita);
                    }
                    else
                    {
                        HistorialCitas.Add(cita);
                    }
                }

                UpdateVisibility();

                await AppUtils.MostrarSnackbar($"Se encontraron {citas.Count} citas.", Colors.Green, Colors.White);
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Ocurrió un error: {ex.Message}", Colors.DarkRed, Colors.White);
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }

        private async Task ActualizarListaEstados()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                var clienteCedula = AuthService.CurrentUser!.Cedula;
                _todasLasCitas = await _reservationService.GetReservationsById(clienteCedula);
                FiltrarPorEstado(_estadoActual);
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Ocurrió un error: {ex.Message}", Colors.DarkRed, Colors.White);
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }

        private void FiltrarPorEstado(string estado)
        {
            _estadoActual = estado;
            var citasFiltradas = _todasLasCitas
                .Where(c => c.Estado?.ToLower() == estado.ToLower())
                .OrderByDescending(c => c.Fecha)
                .ToList();
            CitasCollectionView.ItemsSource = citasFiltradas;
            EmptyStateLayout.IsVisible = citasFiltradas.Count == 0;
            ActualizarEstilosBotones();
            _ = MostrarHintDeslizar();
        }

        private void ActualizarEstilosBotones()
        {
            BtnPendientes.BackgroundColor = _estadoActual == "Pendiente" ? Color.FromArgb("#FF6F91") : Color.FromArgb("#90A4AE");
            BtnCompletadas.BackgroundColor = _estadoActual == "Completada" ? Color.FromArgb("#FF6F91") : Color.FromArgb("#90A4AE");
            BtnCanceladas.BackgroundColor = _estadoActual == "Cancelada" ? Color.FromArgb("#FF6F91") : Color.FromArgb("#90A4AE");
            BtnFinalizadas.BackgroundColor = _estadoActual == "Finalizada" ? Color.FromArgb("#FF6F91") : Color.FromArgb("#90A4AE");
        }
        private async Task MostrarHintDeslizar()
        {
            if (CitasCollectionView.ItemsSource is IEnumerable<CitaModel> citas && citas.Any())
            {
                await Task.Delay(500);

                // ✅ BUSCAR AMBOS HINTS
                var labels = CitasCollectionView.GetVisualTreeDescendants()
                    .OfType<Label>()
                    .Where(l => (l.Text == "⋙ Desliza para editar" || l.Text == "⋘ Desliza para eliminar"))
                    .ToList();

                // ✅ ANIMAR CADA HINT
                foreach (var label in labels)
                {
                    await label.FadeTo(0.8, 300);
                    await Task.Delay(2500);
                    await label.FadeTo(0, 500);
                }
            }
        }
        private void UpdateVisibility()
        {
            HasProximasCitas = ProximasCitas.Count > 0;
            HasHistorialCitas = HistorialCitas.Count > 0;
        }

        private async void EliminarCitaSwipeInvoked(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipeItem && swipeItem.CommandParameter is int citaId)
            {
                // Buscar la cita en la lista filtrada actual
                var citasActuales = CitasCollectionView.ItemsSource as IEnumerable<CitaModel>;
                CitaModel? cita = citasActuales?.FirstOrDefault(c => c.Id == citaId);
                if (cita == null)
                {
                    await AppUtils.MostrarSnackbar("No se puede encontrar la cita seleccionada.", Colors.Red, Colors.White);
                    return;
                }

                // ✅ VALIDACIÓN 1: No eliminar si NO es Pendiente
                if (!string.Equals(cita.Estado, "Pendiente", StringComparison.OrdinalIgnoreCase))
                {
                    await AppUtils.MostrarSnackbar("Solo puedes cancelar citas en estado Pendiente.", Colors.Orange, Colors.White);
                    return;
                }

                // ✅ VALIDACIÓN 2: No eliminar si falta <24 horas
                var horasRestantes = (cita.Fecha - DateTime.UtcNow).TotalHours;
                if (horasRestantes < 24)
                {
                    await AppUtils.MostrarSnackbar($"No puedes cancelar citas con menos de 24 horas de anticipación. Faltan {Math.Round(horasRestantes, 1)} horas.", Colors.Orange, Colors.White);
                    return;
                }

                // ✅ VALIDACIÓN 3: Bloquear citas finalizadas (por seguridad)
                if (string.Equals(cita.Estado, "Finalizada", StringComparison.OrdinalIgnoreCase))
                {
                    await AppUtils.MostrarSnackbar("No puedes eliminar una cita finalizada.", Colors.Orange, Colors.White);
                    return;
                }

                var popup = new CustomAlertPopup($"¿Seguro Que Quieres Cancelar la cita de {cita.Nombre}?");
                bool confirmacion = await popup.ShowAsync(this);
                if (!confirmacion) return;

                try
                {
                    LoadingIndicator.IsVisible = true;
                    LoadingIndicator.IsLoading = true;
                    bool eliminado = await _reservationService.DeleteReservation(cita.Id);

                    if (eliminado)
                    {
                        await AppUtils.MostrarSnackbar("Cita cancelada exitosamente.", Colors.Green, Colors.White);
                        await ActualizarListaEstados(); // Recarga la lista y los tabs
                    }
                    else
                    {
                        await AppUtils.MostrarSnackbar("No se pudo cancelar la cita.", Colors.Red, Colors.White);
                    }
                }
                catch (Exception ex)
                {
                    await AppUtils.MostrarSnackbar($"Error al cancelar: {ex.Message}", Colors.DarkRed, Colors.White);
                }
                finally
                {
                    LoadingIndicator.IsVisible = false;
                    LoadingIndicator.IsLoading = false;
                }
            }
        }

        // ✅ NUEVO MÉTODO: EDITAR CITA
        private async void EditarCitaSwipeInvoked(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipeItem && swipeItem.CommandParameter is CitaModel cita)
            {
                // Validar que pueda editar
                if (!PuedeEditarCita(cita))
                {
                    await AppUtils.MostrarSnackbar(
                    "Solo puedes editar citas pendientes con más de 24 horas.",
                    Colors.Orange, Colors.White);
                    return;
                }

                try
                {
                    LoadingIndicator.IsVisible = true;
                    LoadingIndicator.IsLoading = true;

                    // ✅ Navegar a MainPage en modo edición pasando la cita
                    var reservationService = App.Current!.Handler.MauiContext!.Services
                        .GetRequiredService<ReservationService>();
                    var authService = App.Current!.Handler.MauiContext!.Services
                        .GetRequiredService<AuthService>();
                    var servicioService = App.Current!.Handler.MauiContext!.Services
                        .GetRequiredService<ServicioService>();

                    // ✅ Obtener datos del servicio con la imagen
                    ServicioModel? servicio = null;
                    if (cita.ServicioId.HasValue && cita.ServicioId > 0)
                    {
                        var servicios = await servicioService.GetServiciosAsync();
                        servicio = servicios?.FirstOrDefault(s => s.Id == cita.ServicioId);
                    }

                    // Crear una vista de edición pasando los datos de la cita existente
                    await Navigation.PushAsync(new MainPage(
                        reservationService,
                        authService,
                        null,
                        servicio, // ✅ PASAR EL SERVICIO CON LA IMAGEN
                        cita.Fecha,
                        cita // ✅ PASAR LA CITA PARA EDICIÓN
                    ));
                }
                catch (Exception ex)
                {
                    await AppUtils.MostrarSnackbar($"Error al editar: {ex.Message}", Colors.DarkRed, Colors.White);
                }
                finally
                {
                    LoadingIndicator.IsVisible = false;
                    LoadingIndicator.IsLoading = false;
                }
            }
        }

        // ✅ MÉTODO AUXILIAR: Validar si puede editar
        private bool PuedeEditarCita(CitaModel cita)
        {
            // Solo citas pendientes
            if (!string.Equals(cita.Estado, "Pendiente", StringComparison.OrdinalIgnoreCase))
                return false;

            // Validar que falten al menos 24 horas
            var tiempoRestante = cita.Fecha - DateTime.UtcNow;
            return tiempoRestante.TotalHours >= 24;
        }
    }
}