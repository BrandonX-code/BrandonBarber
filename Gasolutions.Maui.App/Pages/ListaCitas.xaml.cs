using Gasolutions.Maui.App.Models;
using Gasolutions.Maui.App.Services;
using System.Collections.ObjectModel;

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
                var listaReservas = await _reservationService.GetReservations(datePicker.Date);

                CitasFiltradas.Clear();

                if (listaReservas == null || !listaReservas.Any())
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await DisplayAlert("Sin Reservas", "No hay reservas para esta fecha.", "OK");
                    });
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
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Error", "Hubo un problema al recuperar las reservas. Intenta de nuevo.", "OK");
                });
            }
        }


        private async void EliminarCitasSeleccionadas(object sender, EventArgs e)
        {
            var citasAEliminar = CitasFiltradas.Where(c => c.Seleccionado).ToList();

            if (!citasAEliminar.Any())
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Selección Vacía", "Debes seleccionar al menos una cita para eliminar.", "OK");
                });
            }
            else
            {
                foreach (var cita in citasAEliminar)
                {
                    CitasFiltradas.Remove(cita);
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Éxito", "Citas seleccionadas eliminadas.", "OK");
                });
            }
        }



    }
}
