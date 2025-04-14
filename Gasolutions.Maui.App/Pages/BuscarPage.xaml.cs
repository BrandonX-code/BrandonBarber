using CommunityToolkit.Maui.Core;
using Gasolutions.Maui.App.Models;
using Gasolutions.Maui.App.Services;
using System.Collections.ObjectModel;
using Font = Microsoft.Maui.Font;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace Gasolutions.Maui.App.Pages
{
    public partial class BuscarPage : ContentPage
    {
        public ObservableCollection<CitaModel> CitasFiltradas { get; set; } = new();
        private readonly ReservationService _reservationService;
        public BuscarPage(ReservationService reservationService)
        {
            InitializeComponent();
            BindingContext = this;
            ResultadosCollection.ItemsSource = CitasFiltradas;
            _reservationService = reservationService;
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        private async void OnSearchClicked(object sender, EventArgs e)
        {
            await ActualizarLista();
        }

        private async Task ActualizarLista()
        {
            try
            {
                MostrarLoader(true);
                CitasFiltradas.Clear();

                if (string.IsNullOrWhiteSpace(SearchEntry.Text) || !long.TryParse(SearchEntry.Text, out long cedula))
                {
                    await MostrarSnackbar("Ingrese una Cédula válida.", Colors.Orange, Colors.White);
                    return;
                }

                var citas = await _reservationService.GetReservationsById(cedula);

                if (citas == null || !citas.Any())
                {
                    await MostrarSnackbar("No se encontró ninguna cita con esa Cédula.", Colors.Red, Colors.White);
                    return;
                }

                foreach (var cita in citas)
                {
                    CitasFiltradas.Add(cita);
                }
            }
            catch (Exception ex)
            {
                await MostrarSnackbar($"Ocurrió un error: {ex.Message}", Colors.DarkRed, Colors.White);
            }
            finally
            {
                MostrarLoader(false);
            }
        }


        private void OnClearClicked(object sender, EventArgs e)
        {
            SearchEntry.Text = string.Empty;
            CitasFiltradas.Clear();
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
        private async void EliminarCitaClicked(object sender, EventArgs e)
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

    }

}



