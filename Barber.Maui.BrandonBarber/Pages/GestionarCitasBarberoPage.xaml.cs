namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class GestionarCitasBarberoPage : ContentPage
    {
        private readonly ReservationService _reservationService;
        private List<CitaModel> _todasLasCitas = new();
        private string _estadoActual = "Pendiente";

        public GestionarCitasBarberoPage(ReservationService reservationService)
        {
            InitializeComponent();
            _reservationService = reservationService;

            // Suscribirse a notificaciones
            WeakReferenceMessenger.Default.Register<NotificacionRecibidaMessage>(this, async (r, m) =>
            {
                if (m.Tipo == "nueva_cita")
                {
                    await CargarCitas();
                }
            });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarCitas();
        }

        private async Task CargarCitas()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                var barbero = AuthService.CurrentUser;
                _todasLasCitas = await _reservationService.GetReservationsByBarbero(barbero!.Cedula);
                FiltrarPorEstado(_estadoActual);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudieron cargar las citas: {ex.Message}", "OK");
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
                .OrderBy(c => c.Fecha)
                .ToList();

            CitasCollectionView.ItemsSource = citasFiltradas;
            EmptyStateLayout.IsVisible = citasFiltradas.Count == 0;
            // Actualizar botones
            ActualizarEstilosBotones();
        }

        private void ActualizarEstilosBotones()
        {
            BtnPendientes.BackgroundColor = _estadoActual == "Pendiente" ? Color.FromArgb("#FF6F91") : Color.FromArgb("#90A4AE");
            BtnCompletadas.BackgroundColor = _estadoActual == "Completada" ? Color.FromArgb("#FF6F91") : Color.FromArgb("#90A4AE");
            BtnCanceladas.BackgroundColor = _estadoActual == "Cancelada" ? Color.FromArgb("#FF6F91") : Color.FromArgb("#90A4AE");
        }

        private void OnPendientesClicked(object sender, EventArgs e) => FiltrarPorEstado("Pendiente");
        private void OnCompletadasClicked(object sender, EventArgs e) => FiltrarPorEstado("Completada");
        private void OnCanceladasClicked(object sender, EventArgs e) => FiltrarPorEstado("Cancelada");

        private async void OnAceptarCitaClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is CitaModel cita)
            {
                var confirm = await DisplayAlert("Confirmar",
                    $"¿Deseas aceptar la cita de {cita.Nombre}?",
                    "Sí", "No");

                if (confirm)
                {
                    var exito = await _reservationService.ActualizarEstadoCita(cita.Id, "Completada");
                    if (exito)
                    {
                        await AppUtils.MostrarSnackbar("Cita aceptada", Colors.Green, Colors.White);
                        await CargarCitas();
                    }

                }
            }
        }

        private async void OnRechazarCitaClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is CitaModel cita)
            {
                var confirm = await DisplayAlert("Confirmar",
                    $"¿Deseas rechazar la cita de {cita.Nombre}?",
                    "Sí", "No");

                if (confirm)
                {
                    var exito = await _reservationService.ActualizarEstadoCita(cita.Id, "Cancelada");

                    if (exito)
                    {
                        await AppUtils.MostrarSnackbar("Cita rechazada", Colors.Orange, Colors.White);
                        await CargarCitas();
                    }
                }
            }
        }
    }
}