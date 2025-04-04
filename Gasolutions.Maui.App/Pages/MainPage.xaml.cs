using Gasolutions.Maui.App.Models;
using Gasolutions.Maui.App.Pages;
using Gasolutions.Maui.App.Services;

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
            // Animar el contenido principal primero
            await mainContent.FadeTo(1, 500, Easing.CubicInOut);

            // Animar el header y footer simultáneamente
            var headerAnimation = headerGrid.TranslateTo(0, 0, 500, Easing.SpringOut);
            var footerAnimation = footerGrid.TranslateTo(0, 0, 500, Easing.SpringOut);
            await Task.WhenAll(headerAnimation, footerAnimation);

            // Animar el formulario
            await formLayout.FadeTo(1, 300);
            await formLayout.TranslateTo(0, 0, 400, Easing.CubicOut);

            // Animar campos del formulario secuencialmente
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

        // Métodos de animación simplificados
        private async Task AnimateButtonClick(Button button)
        {
            if (button == null) return;

            await button.ScaleTo(0.9, 100);
            await button.ScaleTo(1, 100);
        }

        private async Task ShakeControl(VisualElement control)
        {
            uint timeout = 50;
            double shakeDistance = 10;

            await control.TranslateTo(-shakeDistance, 0, timeout);
            await control.TranslateTo(shakeDistance, 0, timeout);
            await control.TranslateTo(-shakeDistance, 0, timeout);
            await control.TranslateTo(shakeDistance, 0, timeout);
            await control.TranslateTo(0, 0, timeout);
        }

        private async Task MostrarMensajeAnimado(string mensaje)
        {
            // Crear label de mensaje
            Label mensajeLabel = new Label
            {
                Text = mensaje,
                TextColor = Colors.White,
                BackgroundColor = Color.FromArgb("#CC770000"),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                FontAttributes = FontAttributes.Bold,
                Padding = new Thickness(15),
                Opacity = 0
            };

            Frame frameAlerta = new Frame
            {
                BorderColor = Colors.Red,
                BackgroundColor = Colors.Transparent,
                CornerRadius = 10,
                HasShadow = true,
                Content = mensajeLabel,
                HorizontalOptions = LayoutOptions.Fill,
                Margin = new Thickness(20, 5),
                Opacity = 0
            };

            // Añadir al layout y animar
            formLayout.Children.Add(frameAlerta);

            await frameAlerta.FadeTo(1, 300);
            await frameAlerta.ScaleTo(1.05, 150);
            await frameAlerta.ScaleTo(1, 150);

            // Mantener visible por un momento
            await Task.Delay(2500);

            // Hacer fade out y remover
            await frameAlerta.FadeTo(0, 300);
            formLayout.Children.Remove(frameAlerta);
        }

        private async Task MostrarConfirmacionAnimada()
        {
            // Crear Frame de confirmación
            Label mensajeLabel = new Label
            {
                Text = "¡Reserva guardada exitosamente! 👍",
                TextColor = Colors.White,
                BackgroundColor = Color.FromArgb("#CC006600"),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                FontAttributes = FontAttributes.Bold,
                FontSize = 18,
                Padding = new Thickness(15),
                Opacity = 0
            };

            Frame frameConfirmacion = new()
            {
                BorderColor = Colors.Green,
                BackgroundColor = Colors.Transparent,
                CornerRadius = 10,
                HasShadow = true,
                Content = mensajeLabel,
                HorizontalOptions = LayoutOptions.Fill,
                Margin = new Thickness(20, 5),
                Opacity = 0
            };

            // Añadir al layout y animar
            formLayout.Children.Add(frameConfirmacion);

            await frameConfirmacion.FadeTo(1, 300);
            await frameConfirmacion.ScaleTo(1.1, 200);
            await frameConfirmacion.ScaleTo(1, 200);

            // Mantener visible por un momento
            await Task.Delay(2000);

            // Hacer fade out y remover
            await frameConfirmacion.FadeTo(0, 300);
            formLayout.Children.Remove(frameConfirmacion);
        }

        private async Task AnimarSalida()
        {
            await formLayout.FadeTo(0, 300);
            await formLayout.TranslateTo(0, 50, 300);
            await Task.Delay(200);
        }
    }
}