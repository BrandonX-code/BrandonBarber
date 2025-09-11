using System.Text.Json;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class InicioPages : ContentPage
    {
        private readonly AuthService _authService;
        private readonly ServicioService _servicioService; // Inyecta este servicio
        private List<UsuarioModels>? _todosLosBarberos;
        private readonly ReservationService? _reservationService;
        private readonly UsuarioModels? _perfilData;
        public List<UsuarioModels>? TodosLosBarberos
        {
            get => _todosLosBarberos;
            set
            {
                _todosLosBarberos = value;
                OnPropertyChanged(nameof(TodosLosBarberos)); // Asegúrate de implementar INotifyPropertyChanged
            }
        }

        public InicioPages(AuthService authService, ServicioService servicioService)
        {
            InitializeComponent();
            _authService = authService;
            _servicioService = servicioService;
            LoadUserInfo();

            // Suscribirse al evento de calificación
            WeakReferenceMessenger.Default.Register<CalificacionEnviadaMessage>(this, async (r, m) =>
            {
                await LoadBarberos();
            });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (ClienteView.IsVisible)
            {
                //LoadDisponibilidad();
                _ = LoadBarberos();
            }
        }

        private async void CargarServicios()
        {
            try
            {
                var admin = AuthService.CurrentUser;
                var servicios = await _servicioService.GetServiciosAsync();
                ServiciosCarousel.ItemsSource = servicios;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudieron cargar los servicios: " + ex.Message, "OK");
            }
        }

        private async void MainPage(object sender, EventArgs e)
        {
            var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
            var authService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<AuthService>();
            await Navigation.PushAsync(new MainPage(reservationService, authService));
        }

        private async void CitasList(object sender, EventArgs e)
        {
            var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new ListaCitas(reservationService));
        }

        private async void BuscarCitas(object sender, EventArgs e)
        {
            var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new BuscarPage(reservationService));
        }

        private async void PerfilPage(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PerfilPage());
        }

        private async void Galery(object sender, EventArgs e)
        {
            var galeriaService = Handler!.MauiContext!.Services.GetService<GaleriaService>()!;
            var barberoid = Handler.MauiContext.Services.GetService<AuthService>()!;
            await Navigation.PushAsync(new GaleriaPage(galeriaService, barberoid));
        }

        private async void AddGaleri(object sender, EventArgs e)
        {
            var galeriaService = Handler!.MauiContext!.Services.GetService<GaleriaService>()!;
            var barberoid = Handler.MauiContext.Services.GetService<AuthService>()!;
            await Navigation.PushAsync(new GaleriaPage(galeriaService, barberoid));
        }

        // Métodos para el panel de administrador
        private async void AgregarBarbero(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AgregarBarberoPage());
        }

        private async void ListarBarberos(object sender, EventArgs e)
        {
            var authService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<AuthService>();
            await Navigation.PushAsync(new ListarBarberosPage(authService));
        }

        private async void ListarClientes(object sender, EventArgs e)
        {
            var authService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<AuthService>();
            await Navigation.PushAsync(new ListarClientesPage(authService));
        }

        private async void VerCitas(object sender, EventArgs e)
        {
            var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new ListaCitas(reservationService));
        }

        private async void OnInicioClicked(object sender, EventArgs e)
        {
            var serviciosService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ServicioService>();
            await Navigation.PushAsync(new InicioPages(_authService, serviciosService));
        }
        private async void GestionDeServicios(object sender, EventArgs e)
        {
            var serviciosService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ServicioService>();
            await Navigation.PushAsync(new GestionarServiciosPage(serviciosService));
        }
        private async void GestionDeBarberias(object sender, EventArgs e)
        {
            _ = App.Current!.Handler.MauiContext!.Services.GetRequiredService<BarberiaService>();
            // En tu página de administrador, navegar a:
            var gestionPage = new GestionBarberiasPage();
            await Navigation.PushAsync(gestionPage);
        }

        private async void OnBuscarClicked(object sender, EventArgs e)
        {
            var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new BuscarPage(reservationService));
        }

        private async void OnConfiguracionClicked(object sender, EventArgs e)
        {
            var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new ListaCitas(reservationService));
        }

        private async void VerMetricas(object sender, EventArgs e)
        {
            var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
            var authService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<AuthService>();
            await Navigation.PushAsync(new MetricasPage(reservationService, authService));
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
                    case "cliente":
                        ClienteView.IsVisible = true;
                        BarberoView.IsVisible = false;
                        AdminView.IsVisible = false; // Ocultar panel admin
                        GaleriaClienteBorder.IsVisible = true;
                        CargarServicios();
                        CargarBarberos();
                        break;

                    case "barbero":
                        ClienteView.IsVisible = false;
                        BarberoView.IsVisible = true;
                        AdminView.IsVisible = false; // Ocultar panel admin
                        GaleriaClienteBorder.IsVisible = false;
                        break;

                    case "administrador":
                        ClienteView.IsVisible = false;
                        BarberoView.IsVisible = false;
                        AdminView.IsVisible = true; // Mostrar panel admin
                        GaleriaClienteBorder.IsVisible = false;
                        break;

                    default:
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
            var disponibilidadService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<DisponibilidadService>();
            var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new GestionarDisponibilidadPage(disponibilidadService, reservationService));
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
                    var barberos = usuarios?
                        .Where(u => u.Rol!.Equals("barbero", StringComparison.CurrentCultureIgnoreCase)
                                    && u.IdBarberia == admin!.IdBarberia)
                        .ToList() ?? [];

                    TodosLosBarberos = barberos;
                    BarberosCollectionView.ItemsSource = barberos;
                }
                else
                {
                    await DisplayAlert("Error", "No se pudieron cargar los barberos", "OK");
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
                }
                else
                {
                    await DisplayAlert("Error", "No se pudieron cargar los barberos", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al cargar los barberos: {ex.Message}", "OK");
            }
        }
        private async void OnBarberoSelected(object sender, EventArgs e)
        {
            if (sender is Border border && border.BindingContext is UsuarioModels barbero)
            {
                // Animación de toque
                await border.ScaleTo(0.95, 100, Easing.CubicIn);
                await border.ScaleTo(1, 100, Easing.CubicOut);

                // Navegación a la página de detalle
                await Navigation.PushAsync(new BarberoDetailPage(barbero));
            }
        }



    }
}
