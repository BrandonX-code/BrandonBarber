namespace Gasolutions.Maui.App.Pages
{
    public partial class InicioPages : ContentPage
    {
        private readonly AuthService _authService;
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
                LoadDisponibilidad();
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
            //await Navigation.PushAsync(new ListarBarberosPage());
        }

        private async void ListarClientes(object sender, EventArgs e)
        {
            //await Navigation.PushAsync(new ListarClientesPage());
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
        private async void LoadDisponibilidad()
        {
            var disponibilidadService = App.Current.Handler.MauiContext.Services.GetRequiredService<DisponibilidadService>();

            // Obtener la disponibilidad del barbero
            var disponibilidad = await disponibilidadService.GetDisponibilidad(DateTime.Now);

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