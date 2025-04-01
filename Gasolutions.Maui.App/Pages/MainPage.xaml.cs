using Gasolutions.Maui.App.Models;
using Gasolutions.Maui.App.Pages;
using Gasolutions.Maui.App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gasolutions.Maui.App
{
    public partial class MainPage : ContentPage
    {
        private readonly ReservationService _reservationServices;
        public MainPage(ReservationService reservationService)
        {
            InitializeComponent();
            _reservationServices = reservationService;
        }

        private async void OnInicioClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new InicioPages());
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

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            if (_reservationServices == null)
            {
                await DisplayAlert("Error", "No se pudo conectar con el servicio.", "Aceptar");
                return;
            }

            if (string.IsNullOrWhiteSpace(IdEntry.Text) || !int.TryParse(IdEntry.Text, out int id))
            {
                await DisplayAlert("Validación", "Por favor, ingrese un ID válido.", "Aceptar");
                return;
            }

            if (string.IsNullOrWhiteSpace(NombreEntry.Text) || NombreEntry.Text.Length < 2 || NombreEntry.Text.Length > 50)
            {
                await DisplayAlert("Validación", "El Nombre debe tener entre 2 y 50 caracteres.", "Aceptar");
                return;
            }

            if (string.IsNullOrWhiteSpace(TelefonoEntry.Text) || !TelefonoEntry.Text.All(char.IsDigit) || TelefonoEntry.Text.Length != 10)
            {
                await DisplayAlert("Validación", "El Teléfono debe contener 10 dígitos numéricos.", "Aceptar");
                return;
            }

            if (FechaPicker.Date < DateTime.Today)
            {
                await DisplayAlert("Validación", "La fecha de la cita debe ser futura.", "Aceptar");
                return;
            }

            DateTime fechaSeleccionada = FechaPicker.Date.Add(HoraPicker.Time);

            var citasExistentes = await _reservationServices.GetReservations(FechaPicker.Date);

            Console.WriteLine($"🔹 Citas encontradas para {FechaPicker.Date}: {citasExistentes.Count}");
            foreach (var cita in citasExistentes)
            {
                Console.WriteLine($"🔹 Cita existente - Fecha: {cita.Fecha}");
            }

            if (citasExistentes.Any(c => c.Fecha.Date == fechaSeleccionada.Date && c.Fecha.TimeOfDay == fechaSeleccionada.TimeOfDay))
            {
                await DisplayAlert("Error", "Ya existe una cita en esta fecha y hora. Elija otro horario.", "Aceptar");
                return;
            }

            CitaModel nuevaReserva = new CitaModel
            {
                Id = id,
                Nombre = NombreEntry.Text,
                Telefono = TelefonoEntry.Text,
                Fecha = fechaSeleccionada
            };

            bool guardadoExitoso = await _reservationServices.AddReservation(nuevaReserva);

            if (guardadoExitoso)
            {
                await DisplayAlert("Éxito", "La reserva se guardó correctamente.", "Aceptar");
                Limpiarcampos();
            }
            else
            {
                await DisplayAlert("Error", "Hubo un problema al guardar la reserva.", "Aceptar");
            }
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Confirmación", "¿Está seguro que desea cancelar la reserva?", "Sí", "No");

            if (confirm)
            {
                IdEntry.Text = string.Empty;
                NombreEntry.Text = string.Empty;
                TelefonoEntry.Text = string.Empty;
                FechaPicker.Date = DateTime.Today;

                await Navigation.PopToRootAsync();
            }
        }
        private void Limpiarcampos()
        {
            IdEntry.Text = string.Empty;
            NombreEntry.Text = string.Empty;
            TelefonoEntry.Text = string.Empty;
            FechaPicker.Date = DateTime.Today;
            HoraPicker.Time = TimeSpan.Zero;
        }

    }
}