namespace Gasolutions.Maui.App.Pages
{
    public partial class InicioPages : ContentPage
    {
        private readonly AuthService _authService;
        private List<UsuarioModels> _todosLosBarberos;
        public List<UsuarioModels> TodosLosBarberos
        {
            get => _todosLosBarberos;
            set
            {
                _todosLosBarberos = value;
                OnPropertyChanged(nameof(TodosLosBarberos)); // Asegúrate de implementar INotifyPropertyChanged
            }
        }


        public InicioPages(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;

            // Cargar la información del usuario y mostrar la vista correspondiente
            LoadUserInfo();
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

        private async void MainPage(object sender, EventArgs e)
        {
            var reservationService = App.Current.Handler.MauiContext.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new MainPage(reservationService));
        }
        private async void CitasList(object sender, EventArgs e)
        {
            var reservationService = App.Current.Handler.MauiContext.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new ListaCitas(reservationService));
        }
        private async void BuscarCitas(object sender, EventArgs e)
        {
            var reservationService = App.Current.Handler.MauiContext.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new BuscarPage(reservationService));
        }
        private async void PerfilPage(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PerfilPage());
        }
        private async void galery(object sender, EventArgs e)
        {
            var galeriaService = Handler.MauiContext.Services.GetService<GaleriaService>();
            await Navigation.PushAsync(new GaleriaPage(galeriaService));
        }

        private async void AddGaleri(object sender, EventArgs e)
        {
            var galeriaService = Handler.MauiContext.Services.GetService<GaleriaService>();
            await Navigation.PushAsync(new GaleriaPage(galeriaService));
        }

        // Métodos para el panel de administrador
        private async void AgregarBarbero(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AgregarBarberoPage());
        }

        private async void ListarBarberos(object sender, EventArgs e)
        {
            var authService = App.Current.Handler.MauiContext.Services.GetRequiredService<AuthService>();
            await Navigation.PushAsync(new ListarBarberosPage(authService));
        }

        private async void ListarClientes(object sender, EventArgs e)
        {
            var authService = App.Current.Handler.MauiContext.Services.GetRequiredService<AuthService>();
            await Navigation.PushAsync(new ListarClientesPage(authService));
        }

        private async void VerCitas(object sender, EventArgs e)
        {
            var reservationService = App.Current.Handler.MauiContext.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new ListaCitas(reservationService));
        }

        private async void OnInicioClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new InicioPages(_authService));
        }

        private async void OnBuscarClicked(object sender, EventArgs e)
        {
            var reservationService = App.Current.Handler.MauiContext.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new BuscarPage(reservationService));
        }

        private async void OnConfiguracionClicked(object sender, EventArgs e)
        {
            var reservationService = App.Current.Handler.MauiContext.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new ListaCitas(reservationService));
        }
        private void LoadUserInfo()
        {
            if (AuthService.CurrentUser != null)
            {
                // Mostrar información del usuario
                WelcomeLabel.Text = $"Bienvenido, {AuthService.CurrentUser.Nombre}";

                // Ocultar indicador de carga
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;

                // Mostrar la vista correspondiente según el rol
                switch (AuthService.CurrentUser.Rol.ToLower())
                {
                    case "cliente":
                        ClienteView.IsVisible = true;
                        BarberoView.IsVisible = false;
                        AdminView.IsVisible = false; // Ocultar panel admin
                        GaleriaClienteFrame.IsVisible = true;
                        break;

                    case "barbero":
                        ClienteView.IsVisible = false;
                        BarberoView.IsVisible = true;
                        AdminView.IsVisible = false; // Ocultar panel admin
                        GaleriaClienteFrame.IsVisible = false;
                        break;

                    case "administrador":
                        ClienteView.IsVisible = false;
                        BarberoView.IsVisible = false;
                        AdminView.IsVisible = true; // Mostrar panel admin
                        GaleriaClienteFrame.IsVisible = false;
                        break;

                    default:
                        ClienteView.IsVisible = false;
                        BarberoView.IsVisible = false;
                        AdminView.IsVisible = false;
                        GaleriaClienteFrame.IsVisible = false;
                        break;
                }
            }
        }
        // Add this method to the InicioPages class
        private async void GestionarDisponibilidad(object sender, EventArgs e)
        {
            var disponibilidadService = App.Current.Handler.MauiContext.Services.GetRequiredService<DisponibilidadService>();
            var reservationService = App.Current.Handler.MauiContext.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new GestionarDisponibilidadPage(disponibilidadService, reservationService));
        }

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
                    var usuarios = System.Text.Json.JsonSerializer.Deserialize<List<UsuarioModels>>(jsonContent,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var barberos = usuarios?.Where(u => u.Rol.ToLower() == "barbero").ToList() ?? new List<UsuarioModels>();
                    TodosLosBarberos = barberos;
                    Picker.ItemsSource = TodosLosBarberos;
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

        private void Picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            var picker = (Picker)sender;
            int selectedIndex = picker.SelectedIndex;
            if (selectedIndex != -1)
            {
                UsuarioModels barbero = (UsuarioModels)picker.SelectedItem;
                _ = CargarDisponibilidadPorBarberoAsync(barbero.Cedula);
                NoDisponibilidadAlert.IsVisible = true;
                //NoDisponibilidadAlert. (string)picker.ItemsSource[selectedIndex];
            }
        }

        private async Task CargarDisponibilidadPorBarberoAsync(long cedula)
        {
            var disponibilidadService = App.Current.Handler.MauiContext.Services.GetRequiredService<DisponibilidadService>();

            // Obtener la disponibilidad del barbero
            var disponibilidad = await disponibilidadService.GetDisponibilidadPorBarbero(cedula);

            if (disponibilidad != null && disponibilidad.HorariosDict != null && disponibilidad.HorariosDict.Any())
            {
                // Crear una lista de horarios disponibles
                var horariosDisponibles = disponibilidad.HorariosDict
                .Select(h => new
                {
                    Hora = h.Key,
                    Disponible = h.Value ? "Disponible" : "No Disponible"
                }).ToList();

                // Mostrar los horarios en la vista de cliente
                DisponibilidadListView.ItemsSource = horariosDisponibles;
                DisponibilidadListView.IsVisible = true;

                // Ocultar la alerta ya que hay datos de disponibilidad
                NoDisponibilidadAlert.IsVisible = false;
            }
            else
            {
                // Si no hay disponibilidad, mostrar el mensaje de alerta
                DisponibilidadListView.IsVisible = false;
                NoDisponibilidadAlert.IsVisible = true;
            }
        }
    }
}