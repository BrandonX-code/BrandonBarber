namespace Barber.Maui.BrandonBarber
{
    public partial class MainPage : ContentPage
    {
        private readonly ReservationService _reservationServices;
        private readonly AuthService _authService;
        private readonly UsuarioModels? _barberoPreseleccionado;
        private ServicioModel? _servicioSeleccionado; // ✅ NUEVO CAMPO
        private bool _isCancelling = false;

        public MainPage(ReservationService reservationService, AuthService authService,
            UsuarioModels? barberoPreseleccionado = null, ServicioModel? servicioSeleccionado = null)
        {
            InitializeComponent();
            _reservationServices = reservationService;
            _authService = authService;
            _barberoPreseleccionado = barberoPreseleccionado;
            _servicioSeleccionado = servicioSeleccionado; // ✅ GUARDAR SERVICIO
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // ✅ CONFIGURAR SERVICIO ANTES DE LAS ANIMACIONES
            if (_servicioSeleccionado != null)
            {
                servicioBorder.IsVisible = true;
                ServicioImagen.Source = _servicioSeleccionado.Imagen;
                ServicioNombreLabel.Text = _servicioSeleccionado.Nombre;
                ServicioPrecioLabel.Text = $"${_servicioSeleccionado.Precio:N0}";
            }
            else
            {
                servicioBorder.IsVisible = false;
            }

            await StartEntryAnimations();
            await CargarBarberosAsync();

            // LÓGICA PARA BARBERO PRESELECCIONADO
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
            // ✅ ANIMAR SERVICIO BORDER SI ESTÁ VISIBLE
            if (servicioBorder.IsVisible)
            {
                await servicioBorder.FadeTo(1, 300);
                await Task.Delay((int)delay);
            }
            await barberoBorder.FadeTo(1, 300);
            await Task.Delay((int)delay);
            await fechaBorder.FadeTo(1, 300);
            await Task.Delay((int)delay);
            await horaBorder.FadeTo(1, 300);
            await Task.Delay((int)delay);
            await buttonsLayout.FadeTo(1, 300);
        }

        // ✅ NUEVO MÉTODO PARA CAMBIAR SERVICIO
        private async void OnCambiarServicioClicked(object sender, EventArgs e)
        {
            if (_isCancelling) return;
            _isCancelling = true;

            try
            {
                var popup = new CustomAlertPopup("¿Deseas seleccionar otro servicio?");
                bool confirm = await popup.ShowAsync(this);

                if (confirm)
                {
                    // Limpiar servicio seleccionado
                    _servicioSeleccionado = null;
                    servicioBorder.IsVisible = false;

                    // Volver a InicioPages
                    await Navigation.PopAsync();
                }
            }
            finally
            {
                _isCancelling = false;
            }
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
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsLoading = true;
            try
            {
                if (_reservationServices == null)
                {
                    await AppUtils.MostrarSnackbar("No se pudo conectar con el servicio.", Colors.Red, Colors.White);
                    return;
                }

                var usuario = AuthService.CurrentUser;
                if (usuario == null)
                {
                    await AppUtils.MostrarSnackbar("Usuario no autenticado.", Colors.Red, Colors.White);
                    return;
                }

                DateTime fechaSeleccionada = FechaPicker.Date.Add(HoraPicker.Time);
                if (fechaSeleccionada < DateTime.Now)
                {
                    await AppUtils.MostrarSnackbar("La fecha de la cita debe ser futura.", Colors.Orange, Colors.White);
                    return;
                }

                if (BarberoPicker.SelectedItem is not UsuarioModels barberoSeleccionado)
                {
                    await AppUtils.MostrarSnackbar("Debe seleccionar un barbero.", Colors.Orange, Colors.White);
                    return;
                }

                // ✅ VALIDAR QUE HAYA SERVICIO SELECCIONADO
                if (_servicioSeleccionado == null)
                {
                    await AppUtils.MostrarSnackbar("Debe seleccionar un servicio.", Colors.Orange, Colors.White);
                    return;
                }

                // NUEVA VALIDACIÓN: Verificar disponibilidad del barbero
                var disponibilidadService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<DisponibilidadService>();
                var disponibilidad = await disponibilidadService.GetDisponibilidad(FechaPicker.Date, barberoSeleccionado.Cedula);

                if (disponibilidad == null || disponibilidad.HorariosDict == null || !disponibilidad.HorariosDict.Any())
                {
                    await AppUtils.MostrarSnackbar("El barbero no ha configurado su disponibilidad para esta fecha.", Colors.Orange, Colors.White);
                    return;
                }

                // Verificar si la hora seleccionada está en un rango disponible
                var horaSeleccionada = HoraPicker.Time;
                bool horarioDisponible = false;

                foreach (var horario in disponibilidad.HorariosDict)
                {
                    if (horario.Value) // Si el horario está marcado como disponible
                    {
                        var rangoHoras = horario.Key.Split('-');
                        var horaInicio = DateTime.Parse(rangoHoras[0].Trim()).TimeOfDay;
                        var horaFin = DateTime.Parse(rangoHoras[1].Trim()).TimeOfDay;

                        if (horaSeleccionada >= horaInicio && horaSeleccionada < horaFin)
                        {
                            horarioDisponible = true;
                            break;
                        }
                    }
                }

                if (!horarioDisponible)
                {
                    await AppUtils.MostrarSnackbar("El barbero no está disponible en el horario seleccionado.", Colors.Orange, Colors.White);
                    return;
                }

                int idBarberia = usuario.IdBarberia ?? 0;
                var citasDelDia = await _reservationServices.GetReservations(FechaPicker.Date, idBarberia);
                var citasActuales = citasDelDia?.Where(c => c.Fecha.Date == FechaPicker.Date.Date).ToList() ?? [];

                bool cedulaYaRegistrada = citasActuales.Any(c => c.Cedula == usuario.Cedula);
                if (cedulaYaRegistrada)
                {
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
                    BarberoNombre = string.Empty,
                    // ✅ AGREGAR INFO DEL SERVICIO (necesitarás estas propiedades en CitaModel)
                    // ServicioId = _servicioSeleccionado.Id,
                    // ServicioNombre = _servicioSeleccionado.Nombre,
                    // ServicioPrecio = _servicioSeleccionado.Precio
                };

                bool guardadoExitoso = await _reservationServices.AddReservation(nuevaReserva);

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
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            if (_isCancelling)
                return;

            _isCancelling = true;

            if (sender is Button button)
            {
                button.IsEnabled = false;
                await MainPage.AnimateButtonClick(button);
            }

            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
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
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
                _isCancelling = false;

                if (sender is Button btn)
                    btn.IsEnabled = true;
            }
        }

        private void Limpiarcampos()
        {
            FechaPicker.Date = DateTime.Today;
            HoraPicker.Time = TimeSpan.Zero;
            // ✅ LIMPIAR SERVICIO
            _servicioSeleccionado = null;
            servicioBorder.IsVisible = false;
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
    }
}