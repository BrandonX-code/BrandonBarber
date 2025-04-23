namespace Gasolutions.Maui.App.Pages
{
    public partial class ListaCitas : ContentPage
    {
        public ObservableCollection<CitaModel> CitasFiltradas { get; set; } = new();
        private readonly ReservationService _reservationService;
        public DateTime FechaSeleccionada = DateTime.Now;
        public ListaCitas(ReservationService reservationService)
        {
            InitializeComponent();
            BindingContext = this;
            ResultadosCollection.ItemsSource = CitasFiltradas;
            _reservationService = reservationService;
        }

        private async void RecuperarCitasPorFecha(object sender, EventArgs e)
        {
            try
            {
                MostrarLoader(true);

                var listaReservas = await _reservationService.GetReservations(datePicker.Date);

                CitasFiltradas.Clear();

                if (listaReservas == null || !listaReservas.Any())
                {
                    await MostrarSnackbar("No hay reservas para esta fecha.", Colors.DarkRed, Colors.White);
                }
                else
                {
                    foreach (var reserva in listaReservas)
                    {
                        CitasFiltradas.Add(reserva);
                    }
                }
            }
            catch (Exception)
            {
                await MostrarSnackbar("Hubo un problema al recuperar las reservas.", Colors.Red, Colors.White);
            }
            finally
            {
                MostrarLoader(false);

            }
        }

        private async void EliminarCitasSeleccionadas(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is CitaModel cita)
            {
                bool confirm = await DisplayAlert("Confirmar", $"¿Eliminar la cita de {cita.Nombre}?", "Sí", "No");
                if (!confirm) return;

                try
                {
                    MostrarLoader(true);
                    bool eliminado = await _reservationService.DeleteReservation(cita.Id);

                    if (eliminado)
                    {
                        CitasFiltradas.Remove(cita);
                        await MostrarSnackbar("Cita eliminada exitosamente.", Colors.Green, Colors.White);
                    }
                    else
                    {
                        await MostrarSnackbar("No se pudo eliminar la cita.", Colors.Red, Colors.White);
                    }
                }
                catch (Exception ex)
                {
                    await MostrarSnackbar($"Error al eliminar: {ex.Message}", Colors.DarkRed, Colors.White);
                }
                finally
                {
                    MostrarLoader(false);
                }
            }
        }

        private async Task MostrarSnackbar(string mensaje, Color background, Color textColor)
        {
            var snackbarOptions = new SnackbarOptions
            {
                BackgroundColor = background,
                TextColor = textColor,
                CornerRadius = new CornerRadius(30),
                Font = Font.OfSize("Arial", 16),
                CharacterSpacing = 0
            };

            var snackbar = Snackbar.Make(mensaje, duration: TimeSpan.FromSeconds(3), visualOptions: snackbarOptions);
            await snackbar.Show();
        }
        private void MostrarLoader(bool mostrar)
        {
            LoaderOverlay.IsVisible = mostrar;
        }

    }
}
