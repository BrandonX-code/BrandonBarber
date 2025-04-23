namespace Gasolutions.Maui.App.Pages
{
    public partial class BuscarPage : ContentPage, INotifyPropertyChanged
    {
        public ObservableCollection<CitaModel> ProximasCitas { get; set; } = new();
        public ObservableCollection<CitaModel> HistorialCitas { get; set; } = new();

        private bool _hasProximasCitas;
        public bool HasProximasCitas
        {
            get => _hasProximasCitas;
            set
            {
                if (_hasProximasCitas != value)
                {
                    _hasProximasCitas = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _hasHistorialCitas;
        public bool HasHistorialCitas
        {
            get => _hasHistorialCitas;
            set
            {
                if (_hasHistorialCitas != value)
                {
                    _hasHistorialCitas = value;
                    OnPropertyChanged();
                }
            }
        }

        private readonly ReservationService _reservationService;

        public BuscarPage(ReservationService reservationService)
        {
            InitializeComponent();
            BindingContext = this;
            _reservationService = reservationService;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            ProximasCitas.Clear();
            HistorialCitas.Clear();
            UpdateVisibility();
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

                ProximasCitas.Clear();
                HistorialCitas.Clear();

                if (string.IsNullOrWhiteSpace(SearchEntry.Text) || !long.TryParse(SearchEntry.Text, out long cedula))
                {
                    await MostrarSnackbar("Ingrese una Cédula válida.", Colors.Orange, Colors.White);
                    UpdateVisibility();
                    return;
                }

                var citas = await _reservationService.GetReservationsById(cedula);

                if (citas == null || !citas.Any())
                {
                    await MostrarSnackbar("No se encontró ninguna cita con esa Cédula.", Colors.Red, Colors.White);
                    UpdateVisibility();
                    return;
                }

                DateTime now = DateTime.Now.Date;

                foreach (var cita in citas)
                {
                    if (cita.Fecha.Date >= now)
                    {
                        ProximasCitas.Add(cita);
                    }
                    else
                    {
                        HistorialCitas.Add(cita);
                    }
                }

                UpdateVisibility();

                await MostrarSnackbar($"Se encontraron {citas.Count} citas.", Colors.Green, Colors.White);
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

        private void UpdateVisibility()
        {
            HasProximasCitas = ProximasCitas.Count > 0;
            HasHistorialCitas = HistorialCitas.Count > 0;
        }

        private void OnClearClicked(object sender, EventArgs e)
        {
            SearchEntry.Text = string.Empty;
            ProximasCitas.Clear();
            HistorialCitas.Clear();
            UpdateVisibility();
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
                Font = Font.OfSize("Arial", 14),
                CharacterSpacing = 0
            };

            var snackbar = Snackbar.Make(mensaje, duration: TimeSpan.FromSeconds(3), visualOptions: snackbarOptions);
            await snackbar.Show();
        }

        private async void EliminarCitaClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is int citaId)
            {
                CitaModel cita = ProximasCitas.FirstOrDefault(c => c.Id == citaId);
                if (cita == null)
                {
                    await MostrarSnackbar("No se puede encontrar la cita seleccionada.", Colors.Red, Colors.White);
                    return;
                }

                bool confirm = await DisplayAlert("Confirmar", $"¿Seguro Que Quieres Eliminar la cita de {cita.Nombre}?", "Sí", "No");
                if (!confirm) return;

                try
                {
                    MostrarLoader(true);
                    bool eliminado = await _reservationService.DeleteReservation(cita.Id);

                    if (eliminado)
                    {
                        ProximasCitas.Remove(cita);
                        UpdateVisibility();
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