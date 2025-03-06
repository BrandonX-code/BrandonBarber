using Gasolutions.Maui.App.Pages;
using Gasolutions.Maui.App.Services;

namespace Gasolutions.Maui.App
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnInicioClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new InicioPages());
        }

        private async void OnBuscarClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new BuscarPage());
        }

        private async void OnConfiguracionClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ConfiguracionPage());
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(IdEntry.Text) || !int.TryParse(IdEntry.Text, out _))
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


            await SaveReservation();
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

        //private async Task SaveReservation()
        //{
        //    try
        //    {
        //        await DisplayAlert("Éxito", "Su cita ha sido reservada correctamente.", "Aceptar");

        //        // Limpiar los campos
        //        IdEntry.Text = string.Empty;
        //        NombreEntry.Text = string.Empty;
        //        TelefonoEntry.Text = string.Empty;
        //        FechaHoraPicker.Date = DateTime.Today;
        //    }
        //    catch (Exception ex)
        //    {
        //        await DisplayAlert("Error", $"No se pudo reservar la cita: {ex.Message}", "Aceptar");
        //    }
        //}
        private async Task SaveReservation()
        {
            try
            {
                // Crear un objeto Cita con fecha y hora combinadas
                var nuevaCita = new Cita
                {
                    Id = int.Parse(IdEntry.Text),
                    Nombre = NombreEntry.Text,
                    Telefono = TelefonoEntry.Text,
                    Fecha = FechaPicker.Date.Add(HoraPicker.Time) // Combina fecha y hora
                };

                // Guardar la reserva
                ReservationService.AddReservation(nuevaCita);

                await DisplayAlert("Éxito", "Su cita ha sido reservada correctamente.", "Aceptar");

                // Limpiar los campos
                IdEntry.Text = string.Empty;
                NombreEntry.Text = string.Empty;
                TelefonoEntry.Text = string.Empty;
                FechaPicker.Date = DateTime.Today;
                HoraPicker.Time = TimeSpan.Zero; // Reiniciar la hora
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo reservar la cita: {ex.Message}", "Aceptar");
            }
        }


    }
}