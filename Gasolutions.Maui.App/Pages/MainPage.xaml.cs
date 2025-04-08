using Gasolutions.Maui.App.Models;
using Gasolutions.Maui.App.Pages;
using Gasolutions.Maui.App.Services;
using Font = Microsoft.Maui.Font;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

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

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await StartEntryAnimations();
        }

        private async Task StartEntryAnimations()
        {
            await mainContent.FadeTo(1, 500, Easing.CubicInOut);

            var headerAnimation = headerGrid.TranslateTo(0, 0, 500, Easing.SpringOut);
            //var footerAnimation = footerGrid.TranslateTo(0, 0, 500, Easing.SpringOut);
            await Task.WhenAll(headerAnimation);

            await formLayout.FadeTo(1, 300);
            await formLayout.TranslateTo(0, 0, 400, Easing.CubicOut);

            uint delay = 100;
            await idBorder.FadeTo(1, 300);
            await Task.Delay((int)delay);
            await nombreBorder.FadeTo(1, 300);
            await Task.Delay((int)delay);
            await telefonoBorder.FadeTo(1, 300);
            await Task.Delay((int)delay);
            await fechaBorder.FadeTo(1, 300);
            await Task.Delay((int)delay);
            await horaBorder.FadeTo(1, 300);
            await Task.Delay((int)delay);
            await buttonsLayout.FadeTo(1, 300);
        }

        private async void OnInicioClicked(object sender, EventArgs e)
        {
            await AnimateButtonClick(sender as Button);
            await Navigation.PushAsync(new InicioPages());
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

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            MostrarLoader(true);

            try
            {
                if (_reservationServices == null)
                {
                    MostrarLoader(false);
                    await MostrarSnackbar("No se pudo conectar con el servicio.", Colors.Red, Colors.White);
                    return;
                }

                if (string.IsNullOrWhiteSpace(IdEntry.Text) || !int.TryParse(IdEntry.Text, out int id))
                {
                    MostrarLoader(false);
                    await MostrarSnackbar("Por favor, ingrese un ID válido.", Colors.Orange, Colors.White);
                    return;
                }

                if (string.IsNullOrWhiteSpace(NombreEntry.Text) || NombreEntry.Text.Length < 2 || NombreEntry.Text.Length > 50)
                {
                    MostrarLoader(false);
                    await MostrarSnackbar("El Nombre debe tener entre 2 y 50 caracteres.", Colors.Orange, Colors.White);
                    return;
                }

                if (string.IsNullOrWhiteSpace(TelefonoEntry.Text) || !TelefonoEntry.Text.All(char.IsDigit) || TelefonoEntry.Text.Length != 10)
                {
                    MostrarLoader(false);
                    await MostrarSnackbar("El Teléfono debe contener 10 dígitos numéricos.", Colors.Orange, Colors.White);
                    return;
                }

                if (FechaPicker.Date < DateTime.Today)
                {
                    MostrarLoader(false);
                    await MostrarSnackbar("La fecha de la cita debe ser futura.", Colors.Orange, Colors.White);
                    return;
                }


                DateTime fechaSeleccionada = FechaPicker.Date.Add(HoraPicker.Time);
                Console.WriteLine($"Fecha y hora seleccionada: {fechaSeleccionada:yyyy-MM-dd HH:mm:ss}");

                var citasDelDia = await _reservationServices.GetReservations(FechaPicker.Date);
                Console.WriteLine($"Citas encontradas para el día {FechaPicker.Date:yyyy-MM-dd}: {citasDelDia?.Count ?? 0}");

                var citasActuales = citasDelDia?.Where(c => c.Fecha.Date == FechaPicker.Date.Date).ToList() ?? new List<CitaModel>();

                Console.WriteLine($"Citas filtradas solo para fecha actual: {citasActuales.Count}");

                foreach (var cita in citasActuales)
                {
                    Console.WriteLine($"Cita existente: ID={cita.Id}, Fecha={cita.Fecha:yyyy-MM-dd HH:mm:ss}");
                }

                bool conflictoHora = false;
                CitaModel citaConflicto = null;

                foreach (var cita in citasActuales)
                {
                    if (cita.Fecha.Hour == fechaSeleccionada.Hour && cita.Fecha.Minute == fechaSeleccionada.Minute)
                    {
                        conflictoHora = true;
                        citaConflicto = cita;
                        Console.WriteLine($"¡CONFLICTO! Hora existente: {cita.Fecha.Hour}:{cita.Fecha.Minute:D2}");
                        break;
                    }
                }

                if (conflictoHora)
                {
                    MostrarLoader(false);
                    Console.WriteLine($"Mostrando alerta de conflicto para hora {fechaSeleccionada.Hour}:{fechaSeleccionada.Minute:D2}");
                    await MostrarSnackbar("Ya existe una cita en esta fecha y hora. Elija otro horario.", Colors.DarkRed, Colors.White);
                    return;
                }

                CitaModel nuevaReserva = new CitaModel
                {
                    Id = id,
                    Nombre = NombreEntry.Text,
                    Telefono = TelefonoEntry.Text,
                    Fecha = fechaSeleccionada
                };

                Console.WriteLine($"Intentando guardar cita: ID={nuevaReserva.Id}, Fecha={nuevaReserva.Fecha:yyyy-MM-dd HH:mm:ss}");

                bool guardadoExitoso = await _reservationServices.AddReservation(nuevaReserva);
                Console.WriteLine($"Resultado del guardado: {(guardadoExitoso ? "Éxito" : "Fallo")}");

                MostrarLoader(false);

                if (guardadoExitoso)
                {
                    await MostrarSnackbar("La reserva se guardó correctamente.", Colors.Green, Colors.White);
                    Limpiarcampos();
                }
                else
                {
                    var citasActualizadas = await _reservationServices.GetReservations(FechaPicker.Date);

                    var citasFiltradas = citasActualizadas?.Where(c => c.Fecha.Date == FechaPicker.Date.Date).ToList() ?? new List<CitaModel>();

                    bool ahoraHayConflicto = citasFiltradas.Any(c =>
                        c.Fecha.Hour == fechaSeleccionada.Hour &&
                        c.Fecha.Minute == fechaSeleccionada.Minute);

                    if (ahoraHayConflicto)
                    {
                        Console.WriteLine("Se detectó conflicto al verificar después del fallo");
                        await MostrarSnackbar("Ya existe una cita en esta fecha y hora. Elija otro horario.", Colors.DarkRed, Colors.White);
                    }
                    else
                    {
                        Console.WriteLine("Fallo al guardar sin detectar conflicto");
                        await MostrarSnackbar("Hubo un problema al guardar la reserva.", Colors.DarkRed, Colors.White);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"STACK TRACE: {ex.StackTrace}");
                MostrarLoader(false);
                await MostrarSnackbar("Ocurrió un error al procesar la solicitud.", Colors.DarkRed, Colors.White);
            }
        }
        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            await AnimateButtonClick(sender as Button);

            bool confirm = await DisplayAlert("Confirmación", "¿Está seguro que desea cancelar la reserva?", "Sí", "No");

            if (confirm)
            {
                await AnimarSalida();
                Limpiarcampos();
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

        private async Task AnimateButtonClick(Button button)
        {
            if (button == null) return;

            await button.ScaleTo(0.9, 100);
            await button.ScaleTo(1, 100);
        }

        private async Task AnimarSalida()
        {
            await formLayout.FadeTo(0, 300);
            await formLayout.TranslateTo(0, 50, 300);
            await Task.Delay(200);
        }
        private void MostrarLoader(bool mostrar)
        {
            LoaderOverlay.IsVisible = mostrar;
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
    }
}