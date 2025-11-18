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
        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateVisibility();
            _ = ActualizarLista();
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
                    await AppUtils.MostrarSnackbar("No se encontró ninguna cita con esa Cédula.", Colors.Red, Colors.White);
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

        private void UpdateVisibility()
        {
            HasProximasCitas = ProximasCitas.Count > 0;
            HasHistorialCitas = HistorialCitas.Count > 0;
        }

        // Agregar este método a tu clase BuscarPage

        private async void EliminarCitaSwipeInvoked(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipeItem && swipeItem.CommandParameter is int citaId)
            {
                CitaModel? cita = ProximasCitas.FirstOrDefault(c => c.Id == citaId);
                if (cita == null)
                {
                    await AppUtils.MostrarSnackbar("No se puede encontrar la cita seleccionada.", Colors.Red, Colors.White);
                    return;
                }
                var popup = new CustomAlertPopup($"¿Seguro Que Quieres Eliminar la cita de {cita.Nombre}?");
                bool confirmacion = await popup.ShowAsync(this);
                if (!confirmacion) return;

                try
                {
                    LoadingIndicator.IsVisible = true;
                    LoadingIndicator.IsLoading = true;
                    bool eliminado = await _reservationService.DeleteReservation(cita.Id);

                    if (eliminado)
                    {
                        ProximasCitas.Remove(cita);
                        UpdateVisibility();
                        await AppUtils.MostrarSnackbar("Cita eliminada exitosamente.", Colors.Green, Colors.White);
                    }
                    else
                    {
                        await AppUtils.MostrarSnackbar("No se pudo eliminar la cita.", Colors.Red, Colors.White);
                    }
                }
                catch (Exception ex)
                {
                    await AppUtils.MostrarSnackbar($"Error al eliminar: {ex.Message}", Colors.DarkRed, Colors.White);
                }
                finally
                {
                    LoadingIndicator.IsVisible = false;
                    LoadingIndicator.IsLoading = false;
                }
            }
        }
    }
}