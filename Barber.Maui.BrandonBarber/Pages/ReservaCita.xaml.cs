namespace Barber.Maui.BrandonBarber
{
    public partial class MainPage : ContentPage
    {
        private readonly ReservationService _reservationServices;
        private readonly AuthService _authService;
        private readonly UsuarioModels? _barberoPreseleccionado;
        private bool _isCancelling = false;

        public MainPage(ReservationService reservationService, AuthService authService, UsuarioModels? barberoPreseleccionado = null)
        {
            InitializeComponent();
            _reservationServices = reservationService;
            _authService = authService;
            _barberoPreseleccionado = barberoPreseleccionado;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await StartEntryAnimations();
            await CargarBarberosAsync();
            if (_barberoPreseleccionado != null && BarberoPicker.ItemsSource is List<UsuarioModels> barberos)
            {
                BarberoPicker.IsEnabled = false;
                int index = barberos.FindIndex(b => b.Cedula == _barberoPreseleccionado.Cedula);
                if (index >= 0)
                    BarberoPicker.SelectedIndex = index;
            }
            else
            {
                BarberoPicker.IsEnabled = true;
            }
        }

        private async Task StartEntryAnimations()
        {
            await mainContent.FadeTo(1, 500, Easing.CubicInOut);
            await formLayout.FadeTo(1, 300);
            await formLayout.TranslateTo(0, 0, 400, Easing.CubicOut);
            uint delay = 100;
            await Task.Delay((int)delay);
            await barberoBorder.FadeTo(1, 300);
            await Task.Delay((int)delay);
            await fechaBorder.FadeTo(1, 300);
            await Task.Delay((int)delay);
            await horaBorder.FadeTo(1, 300);
            await Task.Delay((int)delay);
            await buttonsLayout.FadeTo(1, 300);
        }

        private async Task CargarBarberosAsync()
        {
            if (AuthService.CurrentUser == null)
            {
                Debug.WriteLine("❌ Usuario no autenticado.");
                return;
            }

            int? idBarberia = AuthService.CurrentUser.IdBarberia;
            var barberos = await _authService.ObtenerBarberos(idBarberia);
            BarberoPicker.ItemsSource = barberos;
            BarberoPicker.SelectedIndex = -1;
        }

        private async Task OnBuscarClicked(object sender, EventArgs e)
        {
            if (sender is Button button)
                await MainPage.AnimateButtonClick(button);
            var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new BuscarPage(reservationService));
        }

        private async void OnConfiguracionClicked(object sender, EventArgs e)
        {
            if (sender is Button button)
                await MainPage.AnimateButtonClick(button);
            var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new ListaCitas(reservationService));
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            MostrarLoader(true);

            try
            {
                if (_reservationServices == null)
                {
                    MostrarLoader(false);
                    await AppUtils.MostrarSnackbar("No se pudo conectar con el servicio.", Colors.Red, Colors.White);
                    return;
                }

                var usuario = AuthService.CurrentUser;
                if (usuario == null)
                {
                    MostrarLoader(false);
                    await AppUtils.MostrarSnackbar("Usuario no autenticado.", Colors.Red, Colors.White);
                    return;
                }

                DateTime fechaSeleccionada = FechaPicker.Date.Add(HoraPicker.Time);
                if (fechaSeleccionada < DateTime.Now)
                {
                    MostrarLoader(false);
                    await AppUtils.MostrarSnackbar("La fecha de la cita debe ser futura.", Colors.Orange, Colors.White);
                    return;
                }

                if (BarberoPicker.SelectedItem is not UsuarioModels barberoSeleccionado)
                {
                    MostrarLoader(false);
                    await AppUtils.MostrarSnackbar("Debe seleccionar un barbero.", Colors.Orange, Colors.White);
                    return;
                }

                int idBarberia = usuario.IdBarberia ?? 0;
                var citasDelDia = await _reservationServices.GetReservations(FechaPicker.Date, idBarberia);
                var citasActuales = citasDelDia?.Where(c => c.Fecha.Date == FechaPicker.Date.Date).ToList() ?? [];

                bool cedulaYaRegistrada = citasActuales.Any(c => c.Cedula == usuario.Cedula);
                if (cedulaYaRegistrada)
                {
                    MostrarLoader(false);
                    await AppUtils.MostrarSnackbar("Ya existe una cita registrada con esta cédula para el día seleccionado.", Colors.OrangeRed, Colors.White);
                    return;
                }
                var cliente = AuthService.CurrentUser;
                CitaModel nuevaReserva = new()
                {
                    Cedula = cliente!.Cedula,
                    Nombre = cliente.Nombre,
                    Telefono = cliente.Telefono,
                    Fecha = fechaSeleccionada,
                    BarberoId = barberoSeleccionado.Cedula,
                    BarberoNombre = string.Empty
                };

                bool guardadoExitoso = await _reservationServices.AddReservation(nuevaReserva);

                MostrarLoader(false);

                if (guardadoExitoso)
                {
                    await AppUtils.MostrarSnackbar("La reserva se guardó correctamente.", Colors.Green, Colors.White);
                    Limpiarcampos();
                    await AnimarSalida();
                    await Navigation.PopToRootAsync();
                }
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar(ex.Message, Colors.DarkRed, Colors.White);
                MostrarLoader(false);
            }
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            if (_isCancelling)
                return; // Evita clics múltiples

            _isCancelling = true;

            if (sender is Button button)
            {
                button.IsEnabled = false;
                await MainPage.AnimateButtonClick(button);
            }

            try
            {
                var popup = new CustomAlertPopup("¿Está seguro que desea cancelar la reserva?");
                bool confirm = await popup.ShowAsync(this);

                if (confirm)
                {
                    await AnimarSalida();
                    Limpiarcampos();
                    await Navigation.PopToRootAsync();
                }
            }
            finally
            {
                _isCancelling = false;

                if (sender is Button btn)
                    btn.IsEnabled = true;
            }
        }

        private void Limpiarcampos()
        {
            FechaPicker.Date = DateTime.Today;
            HoraPicker.Time = TimeSpan.Zero;
        }

        private static async Task AnimateButtonClick(Button button)
        {
            if (button == null) return;

            await button.ScaleTo(0.9, 100);
            await button.ScaleTo(1, 100);
        }

        private async Task AnimarSalida()
        {
            await formLayout.FadeTo(0, 300);
            await formLayout.TranslateTo(0, 50, 300);
            await Task.Delay(200);
        }
        private void MostrarLoader(bool mostrar)
        {
            LoaderOverlay.IsVisible = mostrar;
        }
    }
}