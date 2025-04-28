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

        private async void OnInicioClicked(object sender, EventArgs e)
        {
            await AnimateButtonClick(sender as Button);
            await Navigation.PushAsync(new InicioPages(_authService));
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
        private async Task AnimateButtonClick(Button button)
        {
            if (button == null) return;

            await button.ScaleTo(0.9, 100);
            await button.ScaleTo(1, 100);
        }
        private void LoadUserInfo()
        {
            if (AuthService.CurrentUser != null)
            {
                // Mostrar información del usuario
                WelcomeLabel.Text = $"Bienvenido, {AuthService.CurrentUser.Nombre}";
                //UserTypeLabel.Text = $"Tipo de usuario: {AuthService.CurrentUser.Rol}";

                // Ocultar indicador de carga
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;

                // Mostrar la vista correspondiente según el rol
                switch (AuthService.CurrentUser.Rol.ToLower())
                {
                    case "cliente":
                        ClienteView.IsVisible = true;
                        BarberoView.IsVisible = false;
                        break;
                    case "barbero":
                        ClienteView.IsVisible = false;
                        BarberoView.IsVisible = true;
                        break;
                    case "administrador":
                        ClienteView.IsVisible = false;
                        BarberoView.IsVisible = false;
                        break;
                    default:
                        // Si el rol no coincide con ninguno de los anteriores, mostrar vista de cliente por defecto
                        ClienteView.IsVisible = true;
                        BarberoView.IsVisible = false;
                        break;
                }
            }
        }
        // Add this method to the InicioPages class
        private async void GestionarDisponibilidad(object sender, EventArgs e)
        {
            await AnimateButtonClick(sender as Button);
            var disponibilidadService = App.Current.Handler.MauiContext.Services.GetRequiredService<DisponibilidadService>();
            var reservationService = App.Current.Handler.MauiContext.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new GestionarDisponibilidadPage(disponibilidadService, reservationService));
        }
    }
}
