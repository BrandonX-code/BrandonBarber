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
                .Where(c =>
    {
        var estadoCita = c.Estado?.ToLower() ?? "";
        var estadoBuscado = estado.ToLower();

        // ✅ Aceptar "Confirmada" o "Completada" como equivalentes
        if (estadoBuscado == "confirmada" && (estadoCita == "confirmada" || estadoCita == "completada"))
            return true;

        return estadoCita == estadoBuscado;
    })
                .OrderBy(c => Math.Abs((c.Fecha.Date - DateTime.Today).TotalDays))
                .ToList();

            CitasCollectionView.ItemsSource = citasFiltradas;
            EmptyStateLayout.IsVisible = citasFiltradas.Count == 0;

            ActualizarEstilosBotones();
        }


        private void ActualizarEstilosBotones()
        {
            BtnPendientes.BackgroundColor = _estadoActual == "Pendiente" ? Color.FromArgb("#FF6F91") : Color.FromArgb("#90A4AE");
            BtnCompletadas.BackgroundColor = _estadoActual == "Confirmada" ? Color.FromArgb("#FF6F91") : Color.FromArgb("#90A4AE");
            BtnReagendar.BackgroundColor = _estadoActual == "ReagendarPendiente" ? Color.FromArgb("#FF6F91") : Color.FromArgb("#90A4AE");
            BtnCanceladas.BackgroundColor = _estadoActual == "Cancelada" ? Color.FromArgb("#FF6F91") : Color.FromArgb("#90A4AE");
            BtnFinalizadas.BackgroundColor = _estadoActual == "Finalizada" ? Color.FromArgb("#FF6F91") : Color.FromArgb("#90A4AE");
        }

        private void OnPendientesClicked(object sender, EventArgs e) => FiltrarPorEstado("Pendiente");
        private void OnCompletadasClicked(object sender, EventArgs e) => FiltrarPorEstado("Confirmada");
        private void OnReagendarClicked(object sender, EventArgs e) => FiltrarPorEstado("ReagendarPendiente");
        private void OnCanceladasClicked(object sender, EventArgs e) => FiltrarPorEstado("Cancelada");
        private void OnFinalizadasClicked(object sender, EventArgs e) => FiltrarPorEstado("Finalizada");

        private async void OnAceptarCitaClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is CitaModel cita)
            {
                var popup = new CustomAlertPopup($"¿Deseas aceptar la cita de {cita.Nombre}?");
                bool confirm = await popup.ShowAsync(this);
                if (confirm)
                {
                    var exito = await _reservationService.ActualizarEstadoCita(cita.Id, "Confirmada");
                    if (exito)
                    {
                        await AppUtils.MostrarSnackbar("Cita aceptada", Colors.Green, Colors.White);
                        await CargarCitas();
                    }

                }
            }
        }
        private async void OnCompletarCitaClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is CitaModel cita)
            {
                //var confirm = await DisplayAlert("Confirmar",
                //    $"¿El cliente {cita.Nombre} asistió a la cita?",
                //    "Sí, completada", "No, cancelar");
                var popup = new CustomAlertPopup($"¿El cliente {cita.Nombre} asistió a la cita?");
                bool confirm = await popup.ShowAsync(this);

                string nuevoEstado = confirm ? "Finalizada" : "Cancelada";

                var exito = await _reservationService.ActualizarEstadoCita(cita.Id, nuevoEstado);

                if (exito)
                {
                    await AppUtils.MostrarSnackbar(
                        confirm ? "Cita marcada como finalizada" : "Cita cancelada",
                        Colors.Green,
                        Colors.White);
                    await CargarCitas();
                }
            }
        }
        private async void OnRechazarCitaClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is CitaModel cita)
            {
                var popup = new CustomAlertPopup($"¿Deseas rechazar la cita de {cita.Nombre}?");
                bool confirm = await popup.ShowAsync(this);
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
        private async void OnAceptarReagendarClicked(object sender, EventArgs e)
        {
            if (sender is Button b && b.CommandParameter is CitaModel cita)
            {
                var popup = new CustomAlertPopup(
                    $"¿Aceptar el nuevo horario solicitado por {cita.Nombre}?");
                if (!await popup.ShowAsync(this)) return;

                var ok = await _reservationService
                    .ActualizarEstadoCita(cita.Id, "Confirmada");

                if (ok)
                {
                    await AppUtils.MostrarSnackbar(
                        "Reagendamiento aceptado",
                        Colors.Green,
                        Colors.White);
                    await CargarCitas();
                }
            }
        }

        private async void OnRechazarReagendarClicked(object sender, EventArgs e)
        {
            if (sender is Button b && b.CommandParameter is CitaModel cita)
            {
                var popup = new CustomAlertPopup(
                    $"¿Rechazar el reagendamiento de {cita.Nombre}?");
                if (!await popup.ShowAsync(this)) return;

                var ok = await _reservationService
                    .ActualizarEstadoCita(cita.Id, "Cancelada");

                if (ok)
                {
                    await AppUtils.MostrarSnackbar(
                        "Reagendamiento rechazado",
                        Colors.Orange,
                        Colors.White);
                    await CargarCitas();
                }
            }
        }

    }
}