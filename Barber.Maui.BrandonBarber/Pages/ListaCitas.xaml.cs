using Barber.Maui.BrandonBarber.Controls;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class ListaCitas : ContentPage
    {
        public ObservableCollection<CitaModel> CitasFiltradas { get; set; } = [];
        private readonly ReservationService _reservationService;
        public DateTime FechaSeleccionada = DateTime.Now;
        private readonly BarberiaService? _barberiaService;
        private List<Barberia>? _barberias;
        private int? _barberiaSeleccionadaId = null;
        private int _barberiaSeleccionadaIndex = -1;
        public bool MostrarBarberoInfo { get; set; }
        private bool _isNavigating = false;
        private bool _barberiaButtonLocked = false;
        public ListaCitas(ReservationService reservationService)
        {
            InitializeComponent();
            var user = AuthService.CurrentUser;
            MostrarBarberoInfo = user != null && (user.Rol?.ToLower() == "admin" || user.Rol?.ToLower() == "administrador" || user.Rol?.ToLower() == "cliente");
            bool esAdministrador = user?.Rol?.ToLower() == "admin" || user?.Rol?.ToLower() == "administrador";
            ContadorCitasSection.IsVisible = !esAdministrador;
            TituloCitasLabel.HorizontalOptions = esAdministrador ? LayoutOptions.Center : LayoutOptions.Start;
            BindingContext = this;
            ResultadosCollection.ItemsSource = CitasFiltradas;
            _reservationService = reservationService;
            _barberiaService = Application.Current!.Handler.MauiContext!.Services.GetService<BarberiaService>();
            _ = ActualizarContador();
            CargarBarberias();
        }
        private async void CargarBarberias()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                var user = AuthService.CurrentUser;

                // Solo mostrar selección si es administrador
                if (user?.Rol?.ToLower() == "admin" || user?.Rol?.ToLower() == "administrador")
                {
                    long idAdministrador = user.Cedula;
                    _barberias = await _barberiaService!.GetBarberiasByAdministradorAsync(idAdministrador);
                    PickerSection.IsVisible = _barberias.Count != 0;
                    BarberiaSelectButton.IsVisible = _barberias.Count > 1;

                    // Mostrar botón cambiar solo si hay más de 1 barbería
                    var cambiarButton = this.FindByName<Button>("BarberiaSelectButton");
                    if (cambiarButton != null)
                    {
                        cambiarButton.IsVisible = _barberias.Count > 1;
                        cambiarButton.Text = "Seleccionar";
                    }

                    if (_barberias.Count > 0)
                    {
                        _barberiaSeleccionadaIndex = 0;
                        var barberia = _barberias[0];
                        _barberiaSeleccionadaId = barberia.Idbarberia;
                        BarberiaSelectedLabel.Text = barberia.Nombre ?? "Seleccionar Barbería";
                        BarberiaTelefonoLabel.Text = barberia.Telefono ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(barberia.LogoUrl))
                        {
                            BarberiaLogoImage.Source = barberia.LogoUrl.StartsWith("http")
                                ? ImageSource.FromUri(new Uri(barberia.LogoUrl))
                                : ImageSource.FromFile(barberia.LogoUrl);
                        }
                        else
                        {
                            BarberiaLogoImage.Source = "picture.png";
                        }
                        // Cambiar texto del botón a "Cambiar" porque ya hay una barbería seleccionada
                        if (cambiarButton != null)
                        {
                            cambiarButton.Text = "Cambiar";
                        }
                    }
                    else
                    {
                        _barberiaSeleccionadaId = 0;
                        BarberiaSelectedLabel.Text = "Seleccionar Barbería";
                        BarberiaTelefonoLabel.Text = string.Empty;
                        BarberiaLogoImage.Source = "picture.png";
                    }
                }
                else
                {
                    // Si es barbero, usar su barbería automáticamente
                    _barberiaSeleccionadaId = user?.IdBarberia ?? 0;
                    PickerSection.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudieron cargar las barberías: " + ex.Message, "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }

        private async void OnBarberiaPickerTapped(object sender, EventArgs e)
        {
            if (_barberiaButtonLocked) return;
            _barberiaButtonLocked = true;

            try
            {
                if (_barberias == null || _barberias.Count <= 1)
                    return;

                var popup = new BarberiaSelectionPopup(_barberias);
                var seleccionada = await popup.ShowAsync();

                if (seleccionada != null)
                {
                    int idx = _barberias.FindIndex(b => b.Idbarberia == seleccionada.Idbarberia);
                    if (idx >= 0)
                    {
                        _barberiaSeleccionadaIndex = idx;
                        _barberiaSeleccionadaId = seleccionada.Idbarberia;

                        BarberiaSelectedLabel.Text = seleccionada.Nombre ?? "Seleccionar Barbería";
                        BarberiaTelefonoLabel.Text = seleccionada.Telefono ?? string.Empty;

                        if (!string.IsNullOrWhiteSpace(seleccionada.LogoUrl))
                        {
                            BarberiaLogoImage.Source = seleccionada.LogoUrl.StartsWith("http")
                                ? ImageSource.FromUri(new Uri(seleccionada.LogoUrl))
                                : ImageSource.FromFile(seleccionada.LogoUrl);
                        }
                        else
                        {
                            BarberiaLogoImage.Source = "picture.png";
                        }

                        // Cambiar texto del botón a "Cambiar"
                        var cambiarButton = this.FindByName<Button>("BarberiaSelectButton");
                        if (cambiarButton != null)
                        {
                            cambiarButton.Text = "Cambiar";
                        }

                        // Limpiar citas actuales cuando cambie la barbería
                        CitasFiltradas.Clear();
                    }
                }
                else if (_barberiaSeleccionadaIndex >= 0 && _barberias.Count > _barberiaSeleccionadaIndex)
                {
                    // Restaurar selección anterior si se cancela
                    var barberia = _barberias[_barberiaSeleccionadaIndex];
                    _barberiaSeleccionadaId = barberia.Idbarberia;

                    BarberiaSelectedLabel.Text = barberia.Nombre ?? "Seleccionar Barbería";
                    BarberiaTelefonoLabel.Text = barberia.Telefono ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(barberia.LogoUrl))
                    {
                        BarberiaLogoImage.Source = barberia.LogoUrl.StartsWith("http")
                            ? ImageSource.FromUri(new Uri(barberia.LogoUrl))
                            : ImageSource.FromFile(barberia.LogoUrl);
                    }
                    else
                    {
                        BarberiaLogoImage.Source = "picture.png";
                    }
                }
            }
            finally
            {
                _barberiaButtonLocked = false; // permite clic de nuevo
            }
        }
        private async Task ActualizarContador()
        {
            try
            {
                var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
                var barbero = AuthService.CurrentUser;
                var todasCitas = await reservationService.GetReservationsByBarbero(barbero!.Cedula);
                var ahora = DateTime.Now;
                var citasMes = todasCitas.Where(c => c.Fecha.Year == ahora.Year && c.Fecha.Month == ahora.Month).ToList();

                if (citasMes.Count > 0)
                {
                    TotalCitasLabel.Text = citasMes.Count.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error al cargar las citas del mes: {ex.Message}");
            }
        }
        private async void RecuperarCitasPorFecha(object sender, EventArgs e)
        {
            if (_isNavigating) return; // Sale si ya está navegando

            _isNavigating = true;
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                CitasFiltradas.Clear();

                var user = AuthService.CurrentUser;
                if (user == null)
                {
                    await AppUtils.MostrarSnackbar("Usuario no autenticado.", Colors.DarkRed, Colors.White);
                    return;
                }

                // Validar que haya una barbería seleccionada para administradores
                if ((user.Rol?.ToLower() == "admin" || user.Rol?.ToLower() == "administrador") && _barberiaSeleccionadaId == null)
                {
                    await AppUtils.MostrarSnackbar("Debe seleccionar una barbería.", Colors.DarkRed, Colors.White);
                    return;
                }

                List<CitaModel> listaReservas = [];
                if (user.Rol?.ToLower() == "admin" || user.Rol?.ToLower() == "administrador")
                {
                    listaReservas = await _reservationService.GetReservations(datePicker.Date, _barberiaSeleccionadaId!.Value);
                }
                else if (user.Rol?.ToLower() == "barbero")
                {
                    listaReservas = await _reservationService.GetReservationsByBarberoAndFecha(user.Cedula, datePicker.Date);
                }
                else
                {
                    await AppUtils.MostrarSnackbar("Rol no autorizado para ver reservas.", Colors.DarkRed, Colors.White);
                    return;
                }

                if (listaReservas.Count == 0)
                {
                    await AppUtils.MostrarSnackbar("No hay reservas para esta fecha.", Colors.DarkRed, Colors.White);
                }
                else
                {
                    foreach (var reserva in listaReservas)
                    {
                        reserva.MostrarBarberoInfo = MostrarBarberoInfo;
                        CitasFiltradas.Add(reserva);
                    }
                }
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Hubo un problema al recuperar las reservas: {ex.Message}", Colors.Red, Colors.White);
            }
            finally
            {
                _isNavigating = false;
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
            _ = ActualizarContador();
        }


        private async void EliminarCitasSeleccionadas(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is CitaModel cita)
            {
                var popup = new CustomAlertPopup("¿Seguro Que Quieres Eliminar La Cita?");
                bool confirmacion = await popup.ShowAsync(this);
                if (!confirmacion) return;

                try
                {
                    LoadingIndicator.IsVisible = true;
                    LoadingIndicator.IsLoading = true;
                    bool eliminado = await _reservationService.DeleteReservation(cita.Id);

                    if (eliminado)
                    {
                        CitasFiltradas.Remove(cita);
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
