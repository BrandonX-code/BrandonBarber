using Gasolutions.Maui.App.Models;
using Gasolutions.Maui.App.Services;
using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Font = Microsoft.Maui.Font;

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
            var citasAEliminar = CitasFiltradas.Where(c => c.Seleccionado).ToList();

            if (!citasAEliminar.Any())
            {
                await MostrarSnackbar("Debes seleccionar al menos una cita para eliminar.", Colors.Orange, Colors.White);
            }
            else
            {
                foreach (var cita in citasAEliminar)
                {
                    CitasFiltradas.Remove(cita);
                }

                await MostrarSnackbar("Citas seleccionadas eliminadas.", Colors.Green, Colors.White);
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
