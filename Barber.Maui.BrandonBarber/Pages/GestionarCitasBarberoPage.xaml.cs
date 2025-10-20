using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class GestionarCitasBarberoPage : ContentPage
    {
        private readonly ReservationService _reservationService;

        public GestionarCitasBarberoPage(ReservationService reservationService)
        {
            InitializeComponent();
            _reservationService = reservationService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarCitas();
        }

        private async Task CargarCitas()
        {
            try
            {
                var barberoId = AuthService.CurrentUser!.Cedula;
                Debug.WriteLine($"🔍 Cargando citas para barbero: {barberoId}");

                var citas = await _reservationService.GetReservationsByBarbero(barberoId);

                Debug.WriteLine($"📊 Total de citas obtenidas: {citas?.Count ?? 0}");

                if (citas != null && citas.Any())
                {
                    foreach (var cita in citas)
                    {
                        Debug.WriteLine($"  - Cita: {cita.Nombre}, Fecha: {cita.Fecha}, Estado: {cita.Estado}");
                    }
                }

                // Filtrar solo citas de hoy y futuras que estén pendientes
                var citasGestionar = citas?
                    .Where(c => c.Fecha.Date >= DateTime.Today && c.Estado == "Pendiente")
                    .OrderBy(c => c.Fecha)
                    .ToList() ?? new List<CitaModel>();

                Debug.WriteLine($"✅ Citas a gestionar (pendientes): {citasGestionar.Count}");

                CitasCollection.ItemsSource = citasGestionar;

                if (citasGestionar.Count == 0)
                {
                    await AppUtils.MostrarSnackbar("No hay citas pendientes para gestionar", Colors.Orange, Colors.White);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al cargar citas: {ex.Message}");
                Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                await AppUtils.MostrarSnackbar($"Error al cargar citas: {ex.Message}", Colors.Red, Colors.White);
            }
        }

        private async void OnCompletadaClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is CitaModel cita)
            {
                await ActualizarEstado(cita, "Completada");
            }
        }

        private async void OnCanceladaClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is CitaModel cita)
            {
                var popup = new CustomAlertPopup($"¿Confirmar cancelación de la cita de {cita.Nombre}?");
                bool confirmacion = await popup.ShowAsync(this);

                if (confirmacion)
                {
                    await ActualizarEstado(cita, "Cancelada");
                }
            }
        }

        private async Task ActualizarEstado(CitaModel cita, string nuevoEstado)
        {
            try
            {
                bool exito = await _reservationService.ActualizarEstadoCita(cita.Id, nuevoEstado);

                if (exito)
                {
                    await AppUtils.MostrarSnackbar($"Cita marcada como {nuevoEstado}", Colors.Green, Colors.White);
                    await CargarCitas();
                }
                else
                {
                    await AppUtils.MostrarSnackbar("Error al actualizar el estado", Colors.Red, Colors.White);
                }
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error: {ex.Message}", Colors.Red, Colors.White);
            }
        }
    }
}
