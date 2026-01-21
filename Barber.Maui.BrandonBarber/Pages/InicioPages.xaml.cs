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
            HeaderGrid.SizeChanged += HeaderGrid_SizeChanged;
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
        private void HeaderGrid_SizeChanged(object sender, EventArgs e)
        {
            var grid = (Grid)sender;
            double width = grid.Width;
            double height = grid.Height;

            // Curva hacia abajo: los puntos de control están por debajo del borde inferior
            var pathFigure = new PathFigure
            {
                StartPoint = new Point(0, 0), // Esquina superior izquierda
                Segments = new PathSegmentCollection
                {
                    new LineSegment { Point = new Point(0, height - 30) }, // Borde izquierdo, más arriba
                    new BezierSegment
                    {
                        // Puntos de control por debajo del borde inferior
                        Point1 = new Point(width * 0.3, height + 0),   // Primer punto de control (más abajo)
                        Point2 = new Point(width * 0.7, height + 0),   // Segundo punto de control (más abajo)
                        Point3 = new Point(width, height - 50)         // Fin de la curva, borde derecho, más arriba
                    },
                    new LineSegment { Point = new Point(width, 0) },     // Esquina superior derecha
                    new LineSegment { Point = new Point(0, 0) }          // Cierra la figura
                }
            };

            var pathGeometry = new PathGeometry
            {
                Figures = new PathFigureCollection { pathFigure }
            };

            grid.Clip = pathGeometry;
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
                CargarImagenUsuario();
                // Ocultar indicador de carga
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;

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
                        // 🔍 VALIDACIÓN CRÍTICA: Verificar que el cliente tenga barbería asignada
                        if (AuthService.CurrentUser.IdBarberia == null || AuthService.CurrentUser.IdBarberia == 0)
                        {
                            Debug.WriteLine($"⚠️ ADVERTENCIA: Cliente {AuthService.CurrentUser.Nombre} sin barbería asignada");
                            SuperAdminView.IsVisible = false;
                            ClienteView.IsVisible = false;
                            BarberoView.IsVisible = false;
                            AdminView.IsVisible = false;
                            GaleriaClienteBorder.IsVisible = false;

                            // Mostrar un mensaje de error en la pantalla
                            LoadingIndicator.IsVisible = true;
                            LoadingIndicator.IsLoading = false;

                            // Podemos mostrar un mensaje usando el snackbar o un popup
                            _ = MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                await AppUtils.MostrarSnackbar(
                                    "Tu perfil no tiene una barbería asociada. Por favor, contacta al administrador.",
                                    Colors.Red,
                                    Colors.White
                                );
                            });
                        }
                        else
                        {
                            SuperAdminView.IsVisible = false;
                            ClienteView.IsVisible = true;
                            BarberoView.IsVisible = false;
                            AdminView.IsVisible = false;
                            GaleriaClienteBorder.IsVisible = true;
                            CargarServicios();
                            CargarBarberos();
                        }
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
            else
            {
                Debug.WriteLine("❌ ERROR CRÍTICO: AuthService.CurrentUser es null en LoadUserInfo");
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }

        private async void CargarImagenUsuario()
        {
            try
            {
                if (AuthService.CurrentUser != null && !string.IsNullOrWhiteSpace(AuthService.CurrentUser.ImagenPath))
                {
                    // Verificar si es una URL o una ruta local
                    if (AuthService.CurrentUser.ImagenPath.StartsWith("http"))
                    {
                        UsuarioImagenProfile.Source = ImageSource.FromUri(new Uri(AuthService.CurrentUser.ImagenPath));
                    }
                    else
                    {
                        UsuarioImagenProfile.Source = ImageSource.FromFile(AuthService.CurrentUser.ImagenPath);
                    }
                }
                else
                {
                    // Usar imagen por defecto si no hay imagen de usuario
                    UsuarioImagenProfile.Source = ImageSource.FromFile("usericons.png");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al cargar imagen del usuario: {ex.Message}");
                UsuarioImagenProfile.Source = ImageSource.FromFile("usericons.png");
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
                var authservices = App.Current!.Handler.MauiContext!.Services.GetRequiredService<AuthService>();
                await Navigation.PushAsync(new GestionarDisponibilidadPage(disponibilidadService, reservationService,authservices));
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
                LoadingIndicator.IsLoading = true;
                ContentContainer.IsVisible = false;

                // 🔍 Validar que el usuario actual exista
                if (AuthService.CurrentUser == null)
                {
                    Debug.WriteLine("❌ ERROR: AuthService.CurrentUser es null en LoadBarberos");
                    await AppUtils.MostrarSnackbar("Usuario no autenticado", Colors.Red, Colors.White);
                    return;
                }

                // 🔍 Validar que IdBarberia esté establecida
                if (AuthService.CurrentUser.IdBarberia == null || AuthService.CurrentUser.IdBarberia == 0)
                {
                    Debug.WriteLine($"⚠️ IdBarberia no configurada para cliente. Valor: {AuthService.CurrentUser.IdBarberia}");
                    // Para clientes sin barbería asignada, mostrar mensaje pero no cargar
                    NoBarberosLabel.IsVisible = true;
                    NoBarberosLabel.Text = "Por favor, selecciona una barbería";
                    return;
                }

                var response = await _authService._BaseClient.GetAsync("api/auth");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var usuarios = JsonSerializer.Deserialize<List<UsuarioModels>>(jsonContent, _jsonOptions);

                    if (usuarios == null || usuarios.Count == 0)
                    {
                        Debug.WriteLine("⚠️ No hay usuarios en la respuesta de la API");
                        NoBarberosLabel.IsVisible = true;
                        BarberosCollectionView.IsVisible = false;
                        return;
                    }

                    var barberiaActual = AuthService.CurrentUser.IdBarberia;
                    var barberos = usuarios
                        .Where(u => u.Rol != null 
                                     && u.Rol.Equals("barbero", StringComparison.CurrentCultureIgnoreCase)
                                     && u.IdBarberia == barberiaActual)
                        .ToList();

                    TodosLosBarberos = barberos;
                    BarberosCollectionView.ItemsSource = barberos;

                    Debug.WriteLine($"✅ Barberos cargados: {barberos.Count} para barbería {barberiaActual}");

                    NoBarberosLabel.IsVisible = barberos.Count == 0;
                    BarberosCollectionView.IsVisible = barberos.Count > 0;

                    if (barberos.Count == 0)
                    {
                        NoBarberosLabel.Text = "No hay barberos disponibles en esta barbería";
                    }
                }
                else
                {
                    Debug.WriteLine($"❌ Respuesta de API no exitosa - StatusCode: {response.StatusCode}");
                    await AppUtils.MostrarSnackbar("No se pudieron cargar los barberos", Colors.Red, Colors.White);
                }
            }
            catch (NullReferenceException ex)
            {
                Debug.WriteLine($"❌ NullReferenceException en LoadBarberos: {ex.Message}");
                Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                await AppUtils.MostrarSnackbar("Error: datos incompletos del usuario", Colors.Red, Colors.White);
                NoBarberosLabel.IsVisible = true;
                NoBarberosLabel.Text = "Error al cargar los barberos";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error en LoadBarberos: {ex.Message}");
                Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                await AppUtils.MostrarSnackbar($"Error al cargar barberos: {ex.Message}", Colors.Red, Colors.White);
                NoBarberosLabel.IsVisible = true;
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
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
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                ContentContainer.IsVisible = false;

                // 🔍 Validar que el usuario actual exista
                if (AuthService.CurrentUser == null)
                {
                    Debug.WriteLine("❌ ERROR: AuthService.CurrentUser es null");
                    await AppUtils.MostrarSnackbar("Usuario no autenticado", Colors.Red, Colors.White);
                    return;
                }

                // 🔍 Validar que IdBarberia esté establecida
                if (AuthService.CurrentUser.IdBarberia == null || AuthService.CurrentUser.IdBarberia == 0)
                {
                    Debug.WriteLine($"❌ ERROR: IdBarberia no está establecida. Valor: {AuthService.CurrentUser.IdBarberia}");
                    await AppUtils.MostrarSnackbar("Barbería no configurada para el usuario", Colors.Red, Colors.White);
                    return;
                }

                var response = await _authService._BaseClient.GetAsync("api/auth");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var usuarios = JsonSerializer.Deserialize<List<UsuarioModels>>(jsonContent, _jsonOptions);

                    if (usuarios == null)
                    {
                        Debug.WriteLine("❌ ERROR: La respuesta de la API no contiene usuarios válidos");
                        await AppUtils.MostrarSnackbar("No se pudieron procesar los datos de barberos", Colors.Red, Colors.White);
                        return;
                    }

                    var barberiaActual = AuthService.CurrentUser.IdBarberia;
                    var barberos = usuarios
                        .Where(u => u.Rol != null 
                                     && u.Rol.Equals("barbero", StringComparison.CurrentCultureIgnoreCase)
                   && u.IdBarberia == barberiaActual)
                        .ToList();

                    TodosLosBarberos = barberos;
                    BarberosCollectionView.ItemsSource = barberos;

                    // 🔍 Logging para debugging
                    Debug.WriteLine($"✅ Barberos cargados: {barberos.Count} para barbería {barberiaActual}");

                    // Mostrar/ocultar labels según si hay barberos
                    NoBarberosLabel.IsVisible = barberos.Count == 0;
                    BarberosCollectionView.IsVisible = barberos.Count > 0;
                }
                else
                {
                    Debug.WriteLine($"❌ ERROR: Respuesta de API no exitosa - StatusCode: {response.StatusCode}");
                    await AppUtils.MostrarSnackbar("No se pudieron cargar los barberos", Colors.Red, Colors.White);
                }
            }
            catch (NullReferenceException ex)
            {
                Debug.WriteLine($"❌ NullReferenceException en CargarBarberos: {ex.Message}");
                Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                await AppUtils.MostrarSnackbar("Error al cargar barberos: datos incompletos", Colors.Red, Colors.White);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error general en CargarBarberos: {ex.Message}");
                Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                await AppUtils.MostrarSnackbar($"Error al cargar los barberos: {ex.Message}", Colors.Red, Colors.White);
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
                ContentContainer.IsVisible = true;
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
        private async void OnServicioSelected(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                if (sender is Border border && border.BindingContext is ServicioModel servicio)
                {
                    // Animación de toque
                    await border.ScaleTo(0.95, 100, Easing.CubicIn);
                    await border.ScaleTo(1, 100, Easing.CubicOut);

                    // Mostrar popup de confirmación
                    var popup = new CustomAlertPopup("¿Quieres seleccionar este servicio?");
                    bool confirm = await popup.ShowAsync(this);

                    if (confirm)
                    {
                        // Navegar a MainPage pasando el servicio seleccionado
                        var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
                        var authService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<AuthService>();

                        await Navigation.PushAsync(new MainPage(reservationService, authService, null, servicio));
                    }
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
        private async void GestionarExcepciones(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                await Navigation.PushAsync(new GestionarExcepcionesPage());
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
