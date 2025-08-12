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
                CitasFiltradas.Clear();

                var user = AuthService.CurrentUser;
                if (user == null)
                {
                    await AppUtils.MostrarSnackbar("Usuario no autenticado.", Colors.DarkRed, Colors.White);
                    return;
                }

                List<CitaModel> listaReservas = new();

                if (user.Rol?.ToLower() == "admin" || user.Rol?.ToLower() == "administrador")
                {
                    listaReservas = await _reservationService.GetReservations(datePicker.Date, 0);
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

                if (!listaReservas.Any())
                {
                    await AppUtils.MostrarSnackbar("No hay reservas para esta fecha.", Colors.DarkRed, Colors.White);
                }
                else
                {
                    foreach (var reserva in listaReservas)
                    {
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
                    MostrarLoader(false);
                }
            }
        }

        private void MostrarLoader(bool mostrar)
        {
            LoaderOverlay.IsVisible = mostrar;
        }

    }
}
