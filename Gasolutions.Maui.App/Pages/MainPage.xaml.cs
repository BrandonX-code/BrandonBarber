using System.Globalization;

namespace Gasolutions.Maui.App
{
    public partial class MainPage : ContentPage
    {
        private readonly ReservationService _reservationServices;
        private readonly AuthService _authService;
        public MainPage(ReservationService reservationService, AuthService authService)
        {
            InitializeComponent();
            _reservationServices = reservationService;
            _authService = authService;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            CedulaEntry.TextChanged += OnCedulaEntryTextChanged;
            _ = StartEntryAnimations();
            _ = CargarBarberosAsync();
        }

        private async Task StartEntryAnimations()
        {
            await mainContent.FadeTo(1, 500, Easing.CubicInOut);
            await formLayout.FadeTo(1, 300);
            await formLayout.TranslateTo(0, 0, 400, Easing.CubicOut);
            uint delay = 100;
            await idBorder.FadeTo(1, 300);
            await Task.Delay((int)delay);
            await nombreBorder.FadeTo(1, 300);
            await Task.Delay((int)delay);
            await telefonoBorder.FadeTo(1, 300);
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
            var barberos = await _authService.ObtenerBarberos();
            BarberoPicker.ItemsSource = barberos;
            BarberoPicker.SelectedIndex = -1;
        }

        private async void OnCedulaEntryTextChanged(object sender, TextChangedEventArgs e)
        {
            if (long.TryParse(CedulaEntry.Text, out long cedula) && CedulaEntry.Text.Length >= 6)
            {
                var usuario = await _authService.GetUserByCedula(cedula);

                // Validar si el usuario existe y su rol es cliente
                if (usuario != null && usuario.Rol?.ToLower() == "cliente")
                {
                    NombreEntry.Text = usuario.Nombre;
                    TelefonoEntry.Text = usuario.Telefono;
                    NombreEntry.IsEnabled = false;
                    TelefonoEntry.IsEnabled = false;
                }
                else
                {
                    // Si el usuario no es cliente o no existe, limpiar y habilitar
                    NombreEntry.Text = string.Empty;
                    TelefonoEntry.Text = string.Empty;
                    NombreEntry.IsEnabled = true;
                    TelefonoEntry.IsEnabled = true;
                }
            }
            else
            {
                // Si la cédula no es válida, limpiar y habilitar campos
                NombreEntry.Text = string.Empty;
                TelefonoEntry.Text = string.Empty;
                NombreEntry.IsEnabled = true;
                TelefonoEntry.IsEnabled = true;
            }
        }


        private async void OnBuscarClicked(object sender, EventArgs e)
        {
            await AnimateButtonClick(sender as Button);
            var reservationService = App.Current.Handler.MauiContext.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new BuscarPage(reservationService));
        }

        private async void OnConfiguracionClicked(object sender, EventArgs e)
        {
            await AnimateButtonClick(sender as Button);
            var reservationService = App.Current.Handler.MauiContext.Services.GetRequiredService<ReservationService>();
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

                if (string.IsNullOrWhiteSpace(CedulaEntry.Text) || !long.TryParse(CedulaEntry.Text, out long cedula) || CedulaEntry.Text.Length < 6 || CedulaEntry.Text.Length > 10)
                {
                    MostrarLoader(false);
                    await AppUtils.MostrarSnackbar("Por favor, ingrese una cédula válida (entre 6 y 10 dígitos).", Colors.Orange, Colors.White);
                    return;
                }


                if (string.IsNullOrWhiteSpace(NombreEntry.Text) || NombreEntry.Text.Length < 2 || NombreEntry.Text.Length > 50)
                {
                    MostrarLoader(false);
                    await AppUtils.MostrarSnackbar("El Nombre debe tener entre 2 y 50 caracteres.", Colors.Orange, Colors.White);
                    return;
                }

                if (string.IsNullOrWhiteSpace(TelefonoEntry.Text) || !TelefonoEntry.Text.All(char.IsDigit) || TelefonoEntry.Text.Length != 10)
                {
                    MostrarLoader(false);
                    await AppUtils.MostrarSnackbar("El Teléfono debe contener 10 dígitos numéricos.", Colors.Orange, Colors.White);
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

                Console.WriteLine($"Fecha y hora seleccionada: {fechaSeleccionada:yyyy-MM-dd HH:mm:ss}");

                var citasDelDia = await _reservationServices.GetReservations(FechaPicker.Date);
                Console.WriteLine($"Citas encontradas para el día {FechaPicker.Date:yyyy-MM-dd}: {citasDelDia?.Count ?? 0}");

                var citasActuales = citasDelDia?.Where(c => c.Fecha.Date == FechaPicker.Date.Date).ToList() ?? new List<CitaModel>();

                Console.WriteLine($"Citas filtradas solo para fecha actual: {citasActuales.Count}");

                //bool conflictoHora = false;
                //CitaModel citaConflicto = null;

                //foreach (var cita in citasActuales)
                //{
                //    if (cita.Fecha.Hour == fechaSeleccionada.Hour && cita.Fecha.Minute == fechaSeleccionada.Minute && cita.BarberoId == barberoSeleccionado.Cedula)
                //    {
                //        conflictoHora = true;
                //        citaConflicto = cita;
                //        Console.WriteLine($"¡CONFLICTO! Hora existente: {cita.Fecha.Hour}:{cita.Fecha.Minute:D2}");
                //        break;
                //    }
                //}

                //if (conflictoHora)
                //{
                //    MostrarLoader(false);
                //    Console.WriteLine($"Mostrando alerta de conflicto para hora {fechaSeleccionada.Hour}:{fechaSeleccionada.Minute:D2}");
                //    await AppUtils.MostrarSnackbar("Ya existe una cita en esta fecha y hora. Elija otro horario.", Colors.DarkRed, Colors.White);
                //    return;
                //}
                bool cedulaYaRegistrada = citasActuales.Any(c => c.Cedula == cedula);
                if (cedulaYaRegistrada)
                {
                    MostrarLoader(false);
                    await AppUtils.MostrarSnackbar("Ya existe una cita registrada con esta cédula para el día seleccionado.", Colors.OrangeRed, Colors.White);
                    return;
                }

                CitaModel nuevaReserva = new CitaModel
                {
                    Cedula = cedula,
                    Nombre = NombreEntry.Text,
                    Telefono = TelefonoEntry.Text,
                    Fecha = fechaSeleccionada,
                    BarberoId = barberoSeleccionado.Cedula,
                    BarberoNombre = string.Empty
                };

                Console.WriteLine($"Intentando guardar cita: Cedula={nuevaReserva.Cedula}, Fecha={nuevaReserva.Fecha:yyyy-MM-dd HH:mm:ss}");

                bool guardadoExitoso = await _reservationServices.AddReservation(nuevaReserva);
                Console.WriteLine($"Resultado del guardado: {(guardadoExitoso ? "Éxito" : "Fallo")}");

                MostrarLoader(false);

                if (guardadoExitoso)
                {
                    await AppUtils.MostrarSnackbar("La reserva se guardó correctamente.", Colors.Green, Colors.White);
                    Limpiarcampos();
                    await AnimarSalida();
                    await Navigation.PopToRootAsync();
                }
                //else
                //{
                //    var citasActualizadas = await _reservationServices.GetReservations(FechaPicker.Date);
                //    var citasFiltradas = citasActualizadas?.Where(c => c.Fecha.Date == FechaPicker.Date.Date).ToList() ?? new List<CitaModel>();

                //    bool ahoraHayConflicto = citasFiltradas.Any(c =>
                //        c.Fecha.Hour == fechaSeleccionada.Hour &&
                //        c.Fecha.Minute == fechaSeleccionada.Minute);

                //    if (ahoraHayConflicto)
                //    {
                //        Console.WriteLine("Se detectó conflicto al verificar después del fallo");
                //        await AppUtils.MostrarSnackbar("Ya existe una cita en esta fecha y hora. Elija otro horario.", Colors.DarkRed, Colors.White);
                //    }
                //    else
                //    {
                //        Console.WriteLine("Fallo al guardar sin detectar conflicto");
                        
                //    }
                //}
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar(ex.Message, Colors.DarkRed, Colors.White);
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"STACK TRACE: {ex.StackTrace}");
                MostrarLoader(false);
                //await AppUtils.MostrarSnackbar("Ocurrió un error al procesar la solicitud.", Colors.DarkRed, Colors.White);
            }
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            await AnimateButtonClick(sender as Button);

            bool confirm = await DisplayAlert("Confirmación", "¿Está seguro que desea cancelar la reserva?", "Sí", "No");

            if (confirm)
            {
                await AnimarSalida();
                Limpiarcampos();
                await Navigation.PopToRootAsync();
            }
        }

        private void Limpiarcampos()
        {
            CedulaEntry.Text = string.Empty;
            NombreEntry.Text = string.Empty;
            TelefonoEntry.Text = string.Empty;
            FechaPicker.Date = DateTime.Today;
            HoraPicker.Time = TimeSpan.Zero;
        }

        private async Task AnimateButtonClick(Button button)
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