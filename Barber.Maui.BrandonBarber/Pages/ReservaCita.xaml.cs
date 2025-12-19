using Barber.Maui.BrandonBarber.Controls;

namespace Barber.Maui.BrandonBarber
{
    public partial class MainPage : ContentPage
    {
        private readonly ReservationService _reservationServices;
        private readonly AuthService _authService;
        private readonly UsuarioModels? _barberoPreseleccionado;
        private ServicioModel? _servicioSeleccionado; // ✅ NUEVO CAMPO
        private DateTime _fechaPreseleccionada;
        private bool _isCancelling = false;
        private FranjaHorariaModel? _franjaSeleccionada;
        private List<FranjaHorariaModel>? _todasLasFranjas;
        private List<UsuarioModels>? _barberos;
        private int _barberoSeleccionadoIndex = -1;
        private UsuarioModels? _barberoSeleccionado;
        private bool _barberoPickerLocked = false;
        private CitaModel? _citaEnEdicion; // ✅ NUEVA VARIABLE PARA EDICIÓN
        private bool _esEdicion = false; // ✅ BANDERA PARA MODO EDICIÓN

        public MainPage(ReservationService reservationService, AuthService authService,
            UsuarioModels? barberoPreseleccionado = null, ServicioModel? servicioSeleccionado = null, DateTime fechaPreseleccionada = default, CitaModel? citaEnEdicion = null)
        {
            InitializeComponent();
            _reservationServices = reservationService;
            _authService = authService;
            _barberoPreseleccionado = barberoPreseleccionado;
            _servicioSeleccionado = servicioSeleccionado; // ✅ GUARDAR SERVICIO
            _fechaPreseleccionada = fechaPreseleccionada;
            _citaEnEdicion = citaEnEdicion; // ✅ GUARDAR CITA EN EDICIÓN
            _esEdicion = citaEnEdicion != null; // ✅ DETECTAR MODO EDICIÓN
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            FechaPicker.DateSelected += async (s, e) =>
            {
                LimpiarSeleccionFranja();
                await CargarFranjasDisponibles();
            };

            // ✅ DESHABILITAR FECHAS PASADAS
            FechaPicker.MinimumDate = DateTime.Today;

            // ✅ SI ESTAMOS EN MODO EDICIÓN, CARGAR DATOS DE LA CITA
            if (_esEdicion && _citaEnEdicion != null)
            {
                // Cambiar datos visibles para modo edición
                // MainContent.Title = "Editar Cita"; // No disponible en ContentPage
                // BtnGuardar.Text = "Actualizar"; // No disponible

                // ✅ CARGAR DATOS DEL SERVICIO INCLUYENDO LA IMAGEN
                _servicioSeleccionado = new ServicioModel
                {
                    Id = _citaEnEdicion.ServicioId ?? 0,
                    Nombre = _citaEnEdicion.ServicioNombre,
                    Precio = _citaEnEdicion.ServicioPrecio ?? 0,
                    Imagen = _citaEnEdicion.ServicioImagen // ✅ AGREGAR LA IMAGEN
                };
                _fechaPreseleccionada = _citaEnEdicion.Fecha;
            }

            // ✅ CONFIGURAR SERVICIO
            if (_servicioSeleccionado != null)
            {
                servicioBorder.IsVisible = true;

                // ✅ MANEJO MEJORADO DE LA IMAGEN (URL O RUTA LOCAL)
                if (!string.IsNullOrWhiteSpace(_servicioSeleccionado.Imagen))
                {
                    if (_servicioSeleccionado.Imagen.StartsWith("http"))
                    {
                        ServicioImagen.Source = ImageSource.FromUri(new Uri(_servicioSeleccionado.Imagen));
                    }
                    else
                    {
                        ServicioImagen.Source = ImageSource.FromFile(_servicioSeleccionado.Imagen);
                    }
                }
                else
                {
                    ServicioImagen.Source = "picture.png"; // Imagen por defecto
                }

                ServicioNombreLabel.Text = _servicioSeleccionado.Nombre;
                ServicioPrecioLabel.Text = $"${_servicioSeleccionado.Precio:N0}";
            }
            else
            {
                servicioBorder.IsVisible = false;
            }

            // ✅ CONFIGURAR FECHA PRESELECCIONADA
            if (_fechaPreseleccionada == default || _fechaPreseleccionada.Year < 2000)
            {
                FechaPicker.Date = DateTime.Today;
            }
            else
            {
                FechaPicker.Date = _fechaPreseleccionada;
            }

            await StartEntryAnimations();
            await CargarBarberosAsync();
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
            await franjasBorder.FadeTo(1, 300);
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
                // ✅ SI ESTAMOS EDITANDO, MOSTRAR POPUP DE SELECCIÓN
                if (_esEdicion)
                {
                    var servicioService = App.Current!.Handler.MauiContext!.Services
                        .GetRequiredService<ServicioService>();

                    try
                    {
                        LoadingIndicator.IsVisible = true;
                        LoadingIndicator.IsLoading = true;

                        var servicios = await servicioService.GetServiciosAsync();

                        if (servicios != null && servicios.Count > 0)
                        {
                            // ✅ CREAR UN NUEVO POPUP CADA VEZ
                            var popup = new ServicioSelectionPopup(servicios);
                            var servicioSeleccionado = await popup.ShowAsync();

                            // ✅ VERIFICAR QUE SE SELECCIONÓ UN SERVICIO
                            if (servicioSeleccionado != null)
                            {
                                // ✅ LIMPIAR SELECCIÓN ANTERIOR
                                _servicioSeleccionado = null;
                                servicioBorder.IsVisible = false;

                                // ✅ PEQUEÑO DELAY PARA ASEGURAR ACTUALIZACIÓN
                                await Task.Delay(100);

                                // ✅ ACTUALIZAR CON EL NUEVO SERVICIO
                                _servicioSeleccionado = servicioSeleccionado;
                                servicioBorder.IsVisible = true;

                                // ✅ ACTUALIZAR IMAGEN CON MANEJO DE URLs Y RUTAS LOCALES
                                if (!string.IsNullOrWhiteSpace(_servicioSeleccionado.Imagen))
                                {
                                    if (_servicioSeleccionado.Imagen.StartsWith("http"))
                                    {
                                        ServicioImagen.Source = ImageSource.FromUri(new Uri(_servicioSeleccionado.Imagen));
                                    }
                                    else
                                    {
                                        ServicioImagen.Source = ImageSource.FromFile(_servicioSeleccionado.Imagen);
                                    }
                                }
                                else
                                {
                                    ServicioImagen.Source = "picture.png"; // Imagen por defecto
                                }

                                // ✅ ACTUALIZAR LABELS
                                ServicioNombreLabel.Text = _servicioSeleccionado.Nombre ?? "Servicio sin nombre";
                                ServicioPrecioLabel.Text = $"${_servicioSeleccionado.Precio:N0}";

                                // ✅ LOG PARA DEBUG
                                Debug.WriteLine($"✅ Servicio actualizado: {_servicioSeleccionado.Nombre} - ${_servicioSeleccionado.Precio}");
                            }
                            else
                            {
                                Debug.WriteLine("❌ No se seleccionó ningún servicio");
                            }
                        }
                        else
                        {
                            await AppUtils.MostrarSnackbar("No hay servicios disponibles", Colors.Orange, Colors.White);
                        }
                    }
                    finally
                    {
                        LoadingIndicator.IsVisible = false;
                        LoadingIndicator.IsLoading = false;
                    }
                }
                else
                {
                    // ✅ SI NO ESTAMOS EDITANDO, LIMPIAR Y VOLVER A INICIO
                    var popup = new CustomAlertPopup("¿Deseas seleccionar otro servicio?");
                    bool confirm = await popup.ShowAsync(this);

                    if (confirm)
                    {
                        _servicioSeleccionado = null;
                        servicioBorder.IsVisible = false;
                        await Navigation.PopToRootAsync();
                    }
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
            _barberos = await _authService.ObtenerBarberos(idBarberia);
            BarberoSelectButton.IsVisible = _barberos.Count > 1;

            if (_barberos.Count > 0)
            {
                // Selecciona el primero por defecto
                _barberoSeleccionadoIndex = 0;
                _barberoSeleccionado = _barberos[0];
                BarberoSelectedLabel.Text = _barberoSeleccionado.Nombre ?? "Seleccionar Barbero";
                BarberoTelefonoLabel.Text = _barberoSeleccionado.Telefono ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(_barberoSeleccionado.ImagenPath))
                {
                    BarberoFotoImage.Source = _barberoSeleccionado.ImagenPath.StartsWith("http")
                        ? ImageSource.FromUri(new Uri(_barberoSeleccionado.ImagenPath))
                        : ImageSource.FromFile(_barberoSeleccionado.ImagenPath);
                }
                else
                {
                    BarberoFotoImage.Source = "usericons.png";
                }
            }
            else
            {
                BarberoSelectedLabel.Text = "Seleccionar Barbero";
                BarberoTelefonoLabel.Text = string.Empty;
                BarberoFotoImage.Source = "usericons.png";
                _barberoSeleccionado = null;
            }
            LimpiarSeleccionFranja();
            await CargarFranjasDisponibles();
        }

        private async void OnBarberoPickerTapped(object sender, EventArgs e)
        {
            // ⛔ Evitar doble clic
            if (_barberoPickerLocked) return;
            _barberoPickerLocked = true;

            try
            {
                if (_barberos == null || _barberos.Count <= 1)
                    return;

                var popup = new BarberoSelectionPopup(_barberos);
                var seleccionada = await popup.ShowAsync();

                if (seleccionada != null)
                {
                    int idx = _barberos.FindIndex(b => b.Cedula == seleccionada.Cedula);
                    if (idx >= 0)
                    {
                        _barberoSeleccionadoIndex = idx;
                        _barberoSeleccionado = seleccionada;

                        BarberoSelectedLabel.Text = seleccionada.Nombre ?? "Seleccionar Barbero";
                        BarberoTelefonoLabel.Text = seleccionada.Telefono ?? string.Empty;

                        if (!string.IsNullOrWhiteSpace(seleccionada.ImagenPath))
                        {
                            BarberoFotoImage.Source = seleccionada.ImagenPath.StartsWith("http")
                                ? ImageSource.FromUri(new Uri(seleccionada.ImagenPath))
                                : ImageSource.FromFile(seleccionada.ImagenPath);
                        }
                        else
                        {
                            BarberoFotoImage.Source = "usericons.png";
                        }

                        LimpiarSeleccionFranja();
                        await CargarFranjasDisponibles();
                    }
                }
                else if (_barberoSeleccionadoIndex >= 0 && _barberos.Count > _barberoSeleccionadoIndex)
                {
                    // Restaurar selección anterior al cancelar
                    var barbero = _barberos[_barberoSeleccionadoIndex];
                    _barberoSeleccionado = barbero;

                    BarberoSelectedLabel.Text = barbero.Nombre ?? "Seleccionar Barbero";
                    BarberoTelefonoLabel.Text = barbero.Telefono ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(barbero.ImagenPath))
                    {
                        BarberoFotoImage.Source = barbero.ImagenPath.StartsWith("http")
                            ? ImageSource.FromUri(new Uri(barbero.ImagenPath))
                            : ImageSource.FromFile(barbero.ImagenPath);
                    }
                    else
                    {
                        BarberoFotoImage.Source = "usericons.png";
                    }
                }
            }
            finally
            {
                // 🔓 Permitir clic nuevamente
                _barberoPickerLocked = false;
            }
        }

        private async Task CargarFranjasDisponibles()
        {
            if (_barberoSeleccionado == null)
                return;

            var disponibilidadService = App.Current!.Handler.MauiContext!.Services
                .GetRequiredService<DisponibilidadService>();

            var disponibilidad = await disponibilidadService.GetDisponibilidad(
                FechaPicker.Date, _barberoSeleccionado.Cedula);

            if (disponibilidad == null || !disponibilidad.HorariosDict.Any(h => h.Value))
            {
                await AppUtils.MostrarSnackbar("El barbero no tiene disponibilidad para esta fecha",
                    Colors.Orange, Colors.White);
                LimpiarSeleccionFranja();
                FranjasCollectionView.ItemsSource = null;
                franjasBorder.IsVisible = false;
                return;
            }

            // Generar franjas de 40 minutos
            _todasLasFranjas = disponibilidadService.GenerarFranjasHorarias(disponibilidad.HorariosDict);

            // Obtener citas ya agendadas
            var citasDelDia = await _reservationServices.GetReservations(FechaPicker.Date,
                AuthService.CurrentUser!.IdBarberia ?? 0);

            var citasBarbero = citasDelDia.Where(c => c.BarberoId == _barberoSeleccionado.Cedula).ToList();

            // ✅ NUEVA VALIDACIÓN: Marcar franjas pasadas como no disponibles
            var ahora = DateTime.Now;
            var esFechaHoy = FechaPicker.Date.Date == DateTime.Today;

            foreach (var franja in _todasLasFranjas)
            {
                // Si es hoy y la hora ya pasó, marcar como no disponible
                if (esFechaHoy)
                {
                    var horaFranja = DateTime.Today.Add(franja.HoraInicio);
                    if (horaFranja < ahora)
                    {
                        franja.EstaDisponible = false;
                    }
                }
            }

            // Marcar franjas ocupadas
            foreach (var cita in citasBarbero)
            {
                var horaCita = cita.Fecha.TimeOfDay;
                var franjaOcupada = _todasLasFranjas.FirstOrDefault(f =>
                    horaCita >= f.HoraInicio && horaCita < f.HoraFin);

                if (franjaOcupada != null)
                    franjaOcupada.EstaDisponible = false;
            }

            var franjasDisponibles = _todasLasFranjas
                .Where(f => f.EstaDisponible)
                .ToList();

            // ✅ MOSTRAR MENSAJE SI NO HAY FRANJAS DISPONIBLES
            if (franjasDisponibles.Count == 0)
            {
                FranjasCollectionView.ItemsSource = null;
                FranjasVaciasLabel.IsVisible = true;
                FranjaSeleccionadaLabel.Text = "Ninguna hora seleccionada";
                franjasBorder.IsVisible = true;
                return;
            }

            FranjasVaciasLabel.IsVisible = false;
            FranjasCollectionView.ItemsSource = franjasDisponibles;
            franjasBorder.IsVisible = true;
        }

        private void OnFranjaSeleccionada(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is FranjaHorariaModel franja)
            {
                if (!franja.EstaDisponible)
                {
                    // Deseleccionar si no está disponible
                    FranjasCollectionView.SelectedItem = null;
                    LimpiarSeleccionFranja();
                    _ = AppUtils.MostrarSnackbar("Esta hora ya está ocupada", Colors.Red, Colors.White);
                    return;
                }

                _franjaSeleccionada = franja;
                FranjaSeleccionadaLabel.Text = $"Hora Seleccionada: {franja.HoraTexto}";
            }
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

                // Validar fecha seleccionada
                if (FechaPicker.Date == DateTime.MinValue)
                {
                    await AppUtils.MostrarSnackbar("Debe seleccionar una fecha.", Colors.Orange, Colors.White);
                    return;
                }

                // ✅ Validar franja seleccionada PRIMERO
                if (_franjaSeleccionada == null)
                {
                    await AppUtils.MostrarSnackbar("Debe seleccionar una franja horaria.", Colors.Orange, Colors.White);
                    return;
                }

                if (!_franjaSeleccionada.EstaDisponible)
                {
                    await AppUtils.MostrarSnackbar("La franja seleccionada ya no está disponible.", Colors.Orange, Colors.White);
                    return;
                }

                // ✅ Construir fecha con la franja seleccionada
                DateTime fechaSeleccionadaLocal = FechaPicker.Date.Add(_franjaSeleccionada.HoraInicio);
                DateTime fechaSeleccionada = DateTime.SpecifyKind(fechaSeleccionadaLocal, DateTimeKind.Local).ToUniversalTime();

                if (_barberoSeleccionado == null)
                {
                    await AppUtils.MostrarSnackbar("Debe seleccionar un barbero.", Colors.Orange, Colors.White);
                    return;
                }

                if (_servicioSeleccionado == null)
                {
                    await AppUtils.MostrarSnackbar("Debe seleccionar un servicio.", Colors.Orange, Colors.White);
                    return;
                }

                // ✅ Validar que el día tiene disponibilidad (opcional, ya lo validamos antes)
                var disponibilidadService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<DisponibilidadService>();
                var disponibilidad = await disponibilidadService.GetDisponibilidad(FechaPicker.Date, _barberoSeleccionado.Cedula);

                if (disponibilidad == null || disponibilidad.HorariosDict == null || !disponibilidad.HorariosDict.Any())
                {
                    await AppUtils.MostrarSnackbar("El barbero no ha configurado su disponibilidad para esta fecha.", Colors.Orange, Colors.White);
                    return;
                }

                // ✅ SI NO ES EDICIÓN, validar que no haya otra cita del mismo cliente ese día
                if (!_esEdicion)
                {
                    int idBarberia = usuario.IdBarberia ?? 0;
                    var citasDelDia = await _reservationServices.GetReservations(FechaPicker.Date, idBarberia);
                    var citasActuales = citasDelDia?.Where(c => c.Fecha.Date == FechaPicker.Date.Date).ToList() ?? [];

                    // ✅ VALIDAR QUE NO HAYA CITA DEL CLIENTE ESE DÍA (PROBLEMA 3)
                    bool cedulaYaRegistrada = citasActuales.Any(c => c.Cedula == usuario.Cedula);
                    if (cedulaYaRegistrada)
                    {
                        await AppUtils.MostrarSnackbar("Ya existe una cita registrada con esta cédula para el día seleccionado.", Colors.OrangeRed, Colors.White);
                        return;
                    }
                }

                var cliente = AuthService.CurrentUser;

                // ✅ SI ES EDICIÓN, actualizar la cita existente
                if (_esEdicion && _citaEnEdicion != null)
                {
                    _citaEnEdicion.Fecha = fechaSeleccionada;
                    _citaEnEdicion.BarberoId = _barberoSeleccionado.Cedula;
                    _citaEnEdicion.BarberoNombre = string.Empty;
                    _citaEnEdicion.ServicioId = _servicioSeleccionado.Id;
                    _citaEnEdicion.ServicioNombre = _servicioSeleccionado.Nombre;
                    _citaEnEdicion.ServicioPrecio = _servicioSeleccionado.Precio;
                    // ✅ GUARDAR LA IMAGEN DEL SERVICIO
                    _citaEnEdicion.ServicioImagen = _servicioSeleccionado.Imagen;

                    // ✅ ACTUALIZAR EN EL SERVIDOR
                    bool actualizada = await _reservationServices.UpdateReservation(_citaEnEdicion);

                    if (actualizada)
                    {
                        await AppUtils.MostrarSnackbar("Cita actualizada correctamente.", Colors.Green, Colors.White);
                        Limpiarcampos();
                        await AnimarSalida();
                        await Navigation.PopAsync();
                    }
                    else
                    {
                        await AppUtils.MostrarSnackbar("No se pudo actualizar la cita.", Colors.Red, Colors.White);
                    }
                }
                else
                {
                    // ✅ SI NO ES EDICIÓN, crear nueva cita
                    CitaModel nuevaReserva = new()
                    {
                        Cedula = cliente!.Cedula,
                        Nombre = cliente.Nombre,
                        Telefono = cliente.Telefono,
                        Fecha = fechaSeleccionada, // UTC
                        BarberoId = _barberoSeleccionado.Cedula,
                        BarberoNombre = string.Empty,
                        ServicioId = _servicioSeleccionado.Id,
                        ServicioNombre = _servicioSeleccionado.Nombre,
                        ServicioPrecio = _servicioSeleccionado.Precio,
                        // ✅ AGREGAR LA IMAGEN DEL SERVICIO
                        ServicioImagen = _servicioSeleccionado.Imagen
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
        private void LimpiarSeleccionFranja()
        {
            // Eliminar selección visual
            FranjasCollectionView.SelectedItem = null;

            // Resetear variable interna
            _franjaSeleccionada = null;

            // Resetear Label
            FranjaSeleccionadaLabel.Text = "Ninguna hora seleccionada";
        }

        private void Limpiarcampos()
        {
            FechaPicker.Date = DateTime.Today;
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