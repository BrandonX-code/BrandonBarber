using System.Text.Json;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class InicioPages : ContentPage
    {
        private readonly HttpClient _httpClient;
        private readonly AuthService _authService;
        private readonly ServicioService _servicioService;
        private List<UsuarioModels>? _todosLosBarberos;
        private readonly ReservationService? _reservationService;
        private readonly UsuarioModels? _perfilData;
        private bool _isNavigating = false;
        public List<UsuarioModels>? TodosLosBarberos
        {
            get => _todosLosBarberos;
            set
            {
                _todosLosBarberos = value;
                OnPropertyChanged(nameof(TodosLosBarberos));
            }
        }

        public InicioPages(AuthService authService, ServicioService servicioService)
        {
            InitializeComponent();
            _authService = authService;
            _servicioService = servicioService;
            LoadUserInfo();
            _httpClient = App.Current!.Handler.MauiContext!.Services.GetService<AuthService>()!._BaseClient;
            WeakReferenceMessenger.Default.Register<CalificacionEnviadaMessage>(this, async (r, m) =>
            {
                await LoadBarberos();
            });
        }
        private async Task ActualizarBadgeSolicitudes()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/solicitudes/pendientes");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var solicitudes = JsonSerializer.Deserialize<List<SolicitudAdministrador>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (solicitudes != null && solicitudes.Count > 0)
                    {
                        BadgeSolicitudes.IsVisible = true;
                        NumeroSolicitudes.Text = solicitudes.Count.ToString();
                    }
                    else
                    {
                        BadgeSolicitudes.IsVisible = false;
                    }
                }
                else
                {
                    BadgeSolicitudes.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al cargar contador de solicitudes: {ex.Message}");
                BadgeSolicitudes.IsVisible = false;
            }
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            Application.Current!.UserAppTheme = AppTheme.Light;
            if (ClienteView.IsVisible)
            {
                _ = LoadBarberos();
                CargarServicios();
            }
            if (BarberoView.IsVisible)
            {
                _ = ActualizarBadgeCitasMes();
                _ = ActualizarBadgeGestionarCitasMes();
            }
            if (AdminView.IsVisible)
            {
                _ = ActualizarBadgeVerCitasMes();
            }
            _ = ActualizarBadgeSolicitudes();
        }

        private async Task ActualizarBadgeCitasMes()
        {
            try
            {
                var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
                var barbero = AuthService.CurrentUser;
                if (barbero == null) { BadgeCitasMes.IsVisible = false; return; }
                var todasCitas = await reservationService.GetReservationsByBarbero(barbero.Cedula);
                var ahora = DateTime.Now;
                var citasMes = todasCitas.Where(c => c.Fecha.Year == ahora.Year && c.Fecha.Month == ahora.Month).ToList();
                if (citasMes.Count >0)
                {
                    BadgeCitasMes.IsVisible = true;
                    NumeroCitasMes.Text = citasMes.Count.ToString();
                }
                else
                {
                    BadgeCitasMes.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error al cargar badge de citas del mes: {ex.Message}");
                BadgeCitasMes.IsVisible = false;
            }
        }
        private async Task ActualizarBadgeGestionarCitasMes()
        {
            try
            {
                var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
                var barbero = AuthService.CurrentUser;
                if (barbero == null) { BadgeGestionarCitasMes.IsVisible = false; return; }
                var todasCitas = await reservationService.GetReservationsByBarbero(barbero.Cedula);
                var ahora = DateTime.Now;
                // Solo citas del mes actual y estado pendiente
                var citasMes = todasCitas
                    .Where(c => c.Estado?.ToLower() == "pendiente")
                    .ToList();
                if (citasMes.Count > 0)
                {
                    BadgeGestionarCitasMes.IsVisible = true;
                    NumeroGestionarCitasMes.Text = citasMes.Count.ToString();
                }
                else
                {
                    BadgeGestionarCitasMes.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error al cargar badge de gestionar citas del mes: {ex.Message}");
                BadgeGestionarCitasMes.IsVisible = false;
            }
        }
        private async Task ActualizarBadgeVerCitasMes()
        {
            try
            {
                var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
                var admin = AuthService.CurrentUser;
                if (admin == null) { BadgeVerCitasMes.IsVisible = false; return; }
                var todasCitas = await reservationService.GetReservationsByBarberia(admin.IdBarberia ?? 0);
                var ahora = DateTime.Now;
                var citasMes = todasCitas.Where(c => c.Fecha.Year == ahora.Year && c.Fecha.Month == ahora.Month).ToList();
                if (citasMes.Count >0)
                {
                    BadgeVerCitasMes.IsVisible = true;
                    NumeroVerCitasMes.Text = citasMes.Count.ToString();
                }
                else
                {
                    BadgeVerCitasMes.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error al cargar badge de VerCitas del mes: {ex.Message}");
                BadgeVerCitasMes.IsVisible = false;
            }
        }
        private async void CargarServicios()
        {
            try
            {
                var admin = AuthService.CurrentUser;
                var servicios = await _servicioService.GetServiciosAsync();
                ServiciosCarousel.ItemsSource = servicios;
                NoServiciosLabel.IsVisible = servicios == null || servicios.Count == 0;
                ServiciosCarousel.IsVisible = servicios != null && servicios.Count > 0;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudieron cargar los servicios: " + ex.Message, "OK");
            }
        }

        private async void MainPage(object sender, EventArgs e)
        {

            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
                var authService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<AuthService>();
                await Navigation.PushAsync(new MainPage(reservationService, authService));
            }
            finally
            {
                _isNavigating = false;
            }

        }

        private async void CitasList(object sender, EventArgs e)
        {

            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
                await Navigation.PushAsync(new ListaCitas(reservationService));
            }
            finally
            {
                _isNavigating = false;
            }

        }

        private async void BuscarCitas(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
                await Navigation.PushAsync(new BuscarPage(reservationService));
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private async void PerfilPage(object sender, EventArgs e)
        {

            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                await Navigation.PushAsync(new PerfilPage());
            }
            finally
            {
                _isNavigating = false;
            }

        }

        private async void Galery(object sender, EventArgs e)
        {

            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                var galeriaService = Handler!.MauiContext!.Services.GetService<GaleriaService>()!;
                var barberoid = Handler.MauiContext.Services.GetService<AuthService>()!;
                await Navigation.PushAsync(new GaleriaPage(galeriaService, barberoid));
            }
            finally
            {
                _isNavigating = false;
            }

        }

        private async void AddGaleri(object sender, EventArgs e)
        {

            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                var galeriaService = Handler!.MauiContext!.Services.GetService<GaleriaService>()!;
                var barberoid = Handler.MauiContext.Services.GetService<AuthService>()!;
                await Navigation.PushAsync(new GaleriaPage(galeriaService, barberoid));
            }
            finally
            {
                _isNavigating = false;
            }

        }

        private async void AgregarBarbero(object sender, EventArgs e)
        {

            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                await Navigation.PushAsync(new AgregarBarberoPage());
            }
            finally
            {
                _isNavigating = false;
            }

        }

        private async void ListarBarberos(object sender, EventArgs e)
        {

            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                var authService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<AuthService>();
                await Navigation.PushAsync(new ListarBarberosPage(authService));
            }
            finally
            {
                _isNavigating = false;
            }

        }

        private async void ListarClientes(object sender, EventArgs e)
        {

            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                var authService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<AuthService>();
                await Navigation.PushAsync(new ListarClientesPage(authService));
            }
            finally
            {
                _isNavigating = false;
            }

        }

        private async void VerCitas(object sender, EventArgs e)
        {

            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
                await Navigation.PushAsync(new ListaCitas(reservationService));
            }
            finally
            {
                _isNavigating = false;
            }

        }

        private async void OnInicioClicked(object sender, EventArgs e)
        {

            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                var serviciosService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ServicioService>();
                await Navigation.PushAsync(new InicioPages(_authService, serviciosService));
            }
            finally
            {
                _isNavigating = false;
            }

        }
        private async void GestionDeServicios(object sender, EventArgs e)
        {

            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                var serviciosService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ServicioService>();
                await Navigation.PushAsync(new GestionarServiciosPage(serviciosService));
            }
            finally
            {
                _isNavigating = false;
            }

        }
        private async void GestionDeBarberias(object sender, EventArgs e)
        {

            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                _ = App.Current!.Handler.MauiContext!.Services.GetRequiredService<BarberiaService>();
                // En tu página de administrador, navegar a:
                var gestionPage = new GestionBarberiasPage();
                await Navigation.PushAsync(gestionPage);
            }
            finally
            {
                _isNavigating = false;
            }

        }

        private async void OnBuscarClicked(object sender, EventArgs e)
        {

            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
                await Navigation.PushAsync(new BuscarPage(reservationService));
            }
            finally
            {
                _isNavigating = false;
            }

        }

        private async void OnConfiguracionClicked(object sender, EventArgs e)
        {

            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
                await Navigation.PushAsync(new ListaCitas(reservationService));
            }
            finally
            {
                _isNavigating = false;
            }

        }

        private async void VerMetricas(object sender, EventArgs e)
        {

            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
                var authService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<AuthService>();
                await Navigation.PushAsync(new MetricasPage(reservationService, authService));
            }
            finally
            {
                _isNavigating = false;
            }

        }
        private async void GestionarCitasBarbero(object sender, EventArgs e)
        {

            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
                await Navigation.PushAsync(new GestionarCitasBarberoPage(reservationService));
            }
            finally
            {
                _isNavigating = false;
            }

        }
        private void LoadUserInfo()
        {
            if (AuthService.CurrentUser != null)
            {
                // Mostrar información del usuario
                WelcomeLabel.Text = $"Hola, {AuthService.CurrentUser.Nombre}";

                // Ocultar indicador de carga
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;

                // Mostrar la vista correspondiente según el rol
                switch (AuthService.CurrentUser.Rol!.ToLower())
                {
                    case "superadmin":
                        SuperAdminView.IsVisible = true;
                        ClienteView.IsVisible = false;
                        BarberoView.IsVisible = false;
                        AdminView.IsVisible = false;
                        GaleriaClienteBorder.IsVisible = false;
                        break;
                    case "cliente":
                        SuperAdminView.IsVisible = false;
                        ClienteView.IsVisible = true;
                        BarberoView.IsVisible = false;
                        AdminView.IsVisible = false;
                        GaleriaClienteBorder.IsVisible = true;
                        CargarServicios();
                        CargarBarberos();
                        break;

                    case "barbero":
                        SuperAdminView.IsVisible = false;
                        ClienteView.IsVisible = false;
                        BarberoView.IsVisible = true;
                        AdminView.IsVisible = false;
                        GaleriaClienteBorder.IsVisible = false;
                        break;

                    case "administrador":
                        SuperAdminView.IsVisible = false;
                        ClienteView.IsVisible = false;
                        BarberoView.IsVisible = false;
                        AdminView.IsVisible = true; // Mostrar panel admin
                        GaleriaClienteBorder.IsVisible = false;
                        break;

                    default:
                        SuperAdminView.IsVisible = false;
                        ClienteView.IsVisible = false;
                        BarberoView.IsVisible = false;
                        AdminView.IsVisible = false;
                        GaleriaClienteBorder.IsVisible = false;
                        break;
                }
            }
        }

        // Add this method to the InicioPages class
        private async void GestionarDisponibilidad(object sender, EventArgs e)
        {

            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                var disponibilidadService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<DisponibilidadService>();
                var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
                await Navigation.PushAsync(new GestionarDisponibilidadPage(disponibilidadService, reservationService));
            }
            finally
            {
                _isNavigating = false;
            }

        }
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        private async Task LoadBarberos()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;
                ContentContainer.IsVisible = false;

                var response = await _authService._BaseClient.GetAsync("api/auth");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var usuarios = JsonSerializer.Deserialize<List<UsuarioModels>>(jsonContent, _jsonOptions);

                    var admin = AuthService.CurrentUser;
                    var barberos =usuarios?
                        .Where(u => u.Rol!.Equals("barbero", StringComparison.CurrentCultureIgnoreCase)
                                    && u.IdBarberia == admin!.IdBarberia)
                        .ToList() ?? [];

                    TodosLosBarberos = barberos;
                    BarberosCollectionView.ItemsSource = barberos;
                }
                else
                {
                    await AppUtils.MostrarSnackbar("No se pudieron cargar los barberos", Colors.Red, Colors.White);
                    //await DisplayAlert("Error", "No se pudieron cargar los barberos", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al cargar los barberos: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
                ContentContainer.IsVisible = true;
            }
        }
        private async void Picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            var picker = (Picker)sender;
            int selectedIndex = picker.SelectedIndex;
            if (selectedIndex != -1)
            {
                UsuarioModels barbero = (UsuarioModels)picker.SelectedItem;
                await InicioPages.CargarDisponibilidadesPorBarberoAsync(barbero.Cedula);
            }
        }

        private static async Task CargarDisponibilidadesPorBarberoAsync(long barberoId)
        {
            var disponibilidadService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<DisponibilidadService>();

            // Llama al método que devuelve la lista de disponibilidades para el barbero
            var disponibilidades = await disponibilidadService.GetDisponibilidadActualPorBarbero(barberoId);

            if (disponibilidades != null && disponibilidades.Count != 0)
            {
                // Puedes adaptar esto según cómo quieras mostrar los horarios
                var horariosDisponibles = disponibilidades
                    .SelectMany(d => d.HorariosDict.Select(h => new
                    {
                        Fecha = d.Fecha.ToString("yyyy-MM-dd"),
                        Hora = h.Key,
                        Disponible = h.Value ? "Disponible" : "No Disponible"
                    }))
                    .ToList();
            }
        }
        private async void CargarBarberos()
        {
            try
            {
                var usuario = AuthService.CurrentUser!.IdBarberia ?? 0;
                var response = await _authService._BaseClient.GetAsync($"api/auth/barberos/{usuario}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var usuarios = JsonSerializer.Deserialize<List<UsuarioModels>>(jsonContent, _jsonOptions);

                    var admin = AuthService.CurrentUser;
                    var barberos = usuarios?
                        .Where(u => u.Rol!.Equals("barbero", StringComparison.CurrentCultureIgnoreCase)
                                    && u.IdBarberia == admin.IdBarberia)
                        .ToList() ?? [];

                    BarberosCollectionView.ItemsSource = barberos;
                    NoBarberosLabel.IsVisible = barberos.Count == 0;
                    BarberosCollectionView.IsVisible = barberos.Count > 0;
                }
                else
                {
                    await AppUtils.MostrarSnackbar("No se pudieron cargar los barberos", Colors.Red, Colors.White);
                    //await DisplayAlert("Error", "No se pudieron cargar los barberos", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al cargar los barberos: {ex.Message}", "OK");
            }
        }
        private async void OnBarberoSelected(object sender, EventArgs e)
        {

            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                if (sender is Border border && border.BindingContext is UsuarioModels barbero)
                {
                    // Animación de toque
                    await border.ScaleTo(0.95, 100, Easing.CubicIn);
                    await border.ScaleTo(1, 100, Easing.CubicOut);

                    await Navigation.PushAsync(new BarberoDetailPage(barbero));
                }
            }
            finally
            {
                _isNavigating = false;
            }
            
        }

        private async void VerSolicitudesAdmin(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                await Navigation.PushAsync(new GestionSolicitudesPage());
                // Al volver de la página, actualizar el badge
                await ActualizarBadgeSolicitudes();
            }
            finally
            {
                _isNavigating = false;
            }
        }

        // Métodos para nuevas opciones de SuperAdminView
        private async void GestionarAdministradores(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                await Navigation.PushAsync(new GestionarAdministradoresPage());
            }
            finally { _isNavigating = false; }
        }

        private async void VerTodasBarberias(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                await Navigation.PushAsync(new ListarTodasBarberiasPage());
            }
            finally { _isNavigating = false; }
        }
        private async void VerHistorialSolicitudes(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                await Navigation.PushAsync(new HistorialSolicitudesPage());
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private async void BarberiasReportadas(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                // TODO: Navegar a la página de barberías reportadas
                await DisplayAlert("SuperAdmin", "Barberías Reportadas (pendiente de implementar)", "OK");
            }
            finally { _isNavigating = false; }
        }

        private async void TodosUsuarios(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                await Navigation.PushAsync(new ListarTodosUsuariosPage());
            }
            finally { _isNavigating = false; }
        }
    }
}
