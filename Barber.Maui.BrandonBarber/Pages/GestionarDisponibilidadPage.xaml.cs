using Switch = Microsoft.Maui.Controls.Switch;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class GestionarDisponibilidadPage : ContentPage
    {
        private readonly DisponibilidadService _disponibilidadService;
        private DisponibilidadSemanalModel? _disponibilidad;
        private readonly AuthService _authService;
        private bool _isNavigating = false;

        public GestionarDisponibilidadPage(DisponibilidadService disponibilidadService, ReservationService reservationService, AuthService authService)
        {
            InitializeComponent();
            _disponibilidadService = disponibilidadService;
            _authService = authService;

            Appearing += async (_, __) =>
            {
                if (AuthService.CurrentUser == null)
                    await _authService.LoadStoredUserAsync();

                await CargarDisponibilidad();
            };
        }

        private async Task CargarDisponibilidad()
        {
            var barberoId = AuthService.CurrentUser?.Cedula ??0;
            // Siempre obtener desde la API, no desde SecureStorage
            _disponibilidad = await _disponibilidadService.ObtenerDisponibilidadSemanalDesdeApi(barberoId);
            if (_disponibilidad != null)
            {
                GenerarVista();
            }
        }

        private void GenerarVista()
        {
            DiasContainer.Children.Clear();

            foreach (var dia in _disponibilidad!.Dias)
            {
                var border = new Border
                {
                    BackgroundColor = Color.FromArgb("#FFFFFF"),
                    StrokeShape = new RoundRectangle { CornerRadius =15 },
                    Padding = new Thickness(20,15),
                    StrokeThickness =0,
                    Margin = new Thickness(0,0,0,5)
                };

                var mainStack = new VerticalStackLayout
                {
                    Spacing =15
                };

                var headerGrid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = GridLength.Auto }
                    }
                };

                var labelDia = new Label
                {
                    Text = dia.NombreDia,
                    FontSize =18,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.Black,
                    VerticalOptions = LayoutOptions.Center
                };

                var switchHabilitar = new Switch
                {
                    IsToggled = dia.Habilitado,
                    OnColor = Color.FromArgb("#FF6F91"),
                    ThumbColor = Color.FromArgb("#265d82"),
                    VerticalOptions = LayoutOptions.Center
                };

                Grid.SetColumn(labelDia,0);
                Grid.SetColumn(switchHabilitar,1);

                headerGrid.Children.Add(labelDia);
                headerGrid.Children.Add(switchHabilitar);

                var horariosGrid = new Grid
                {
                    IsVisible = dia.Habilitado,
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                    },
                    ColumnSpacing =15
                };

                var inicioStack = new VerticalStackLayout { Spacing =8 };

                var labelInicio = new Label
                {
                    Text = "Inicio",
                    FontSize =14,
                    TextColor = Colors.Black,
                    FontAttributes = FontAttributes.Bold
                };

                var borderInicio = new Border
                {
                    BackgroundColor = Color.FromArgb("#265d82"),
                    StrokeShape = new RoundRectangle { CornerRadius =10 },
                    StrokeThickness =0,
                    Padding = new Thickness(15,8)
                };

                var timePickerInicio = new TimePicker
                {
                    Time = dia.HoraInicio,
                    TextColor = Colors.White,
                    BackgroundColor = Colors.Transparent,
                    FontSize =16,
                    Format = "hh:mm tt",
                    HorizontalOptions = LayoutOptions.Fill
                };

                timePickerInicio.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(TimePicker.Time))
                    {
                        dia.HoraInicio = timePickerInicio.Time;
                    }
                };

                borderInicio.Content = timePickerInicio;
                inicioStack.Children.Add(labelInicio);
                inicioStack.Children.Add(borderInicio);

                var finStack = new VerticalStackLayout { Spacing =8 };

                var labelFin = new Label
                {
                    Text = "Fin",
                    FontSize =14,
                    TextColor = Colors.Black,
                    FontAttributes = FontAttributes.Bold
                };

                var borderFin = new Border
                {
                    BackgroundColor = Color.FromArgb("#265d82"),
                    StrokeShape = new RoundRectangle { CornerRadius =10 },
                    StrokeThickness =0,
                    Padding = new Thickness(15,8)
                };

                var timePickerFin = new TimePicker
                {
                    Time = dia.HoraFin,
                    TextColor = Colors.White,
                    BackgroundColor = Colors.Transparent,
                    FontSize =16,
                    HorizontalOptions = LayoutOptions.Fill
                };

                timePickerFin.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(TimePicker.Time))
                    {
                        dia.HoraFin = timePickerFin.Time;
                    }
                };

                borderFin.Content = timePickerFin;
                finStack.Children.Add(labelFin);
                finStack.Children.Add(borderFin);

                Grid.SetColumn(inicioStack,0);
                Grid.SetColumn(finStack,1);

                horariosGrid.Children.Add(inicioStack);
                horariosGrid.Children.Add(finStack);

                switchHabilitar.Toggled += (s, e) =>
                {
                    dia.Habilitado = e.Value;
                    horariosGrid.IsVisible = e.Value;
                };

                mainStack.Children.Add(headerGrid);
                mainStack.Children.Add(horariosGrid);

                border.Content = mainStack;
                DiasContainer.Children.Add(border);
            }
        }

        // NUEVO: Guardar solo semana o todo el mes
        private async void OnGuardarSemanaClicked(object sender, EventArgs e)
        {
            await GuardarDisponibilidadAsync(aplicarMes: false);
        }

        private async void OnGuardarMesClicked(object sender, EventArgs e)
        {
            await GuardarDisponibilidadAsync(aplicarMes: true);
        }

        private async Task GuardarDisponibilidadAsync(bool aplicarMes)
        {
            if (_isNavigating) return;
            _isNavigating = true;

            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                foreach (var dia in _disponibilidad!.Dias.Where(d => d.Habilitado))
                {
                    if (dia.HoraFin <= dia.HoraInicio)
                    {
                        await DisplayAlert("Error", $"La hora de fin debe ser mayor que la hora de inicio en {dia.NombreDia}", "OK");
                        return;
                    }
                }

                bool guardado;
                if (aplicarMes)
                {
                    guardado = await _disponibilidadService.AplicarDisponibilidadSemanalAMesApi(_disponibilidad.BarberoId, DateTime.Today, _disponibilidad);
                }
                else
                {
                    guardado = await _disponibilidadService.AplicarDisponibilidadSemanalASemanaActualApi(_disponibilidad.BarberoId, DateTime.Today, _disponibilidad);
                }

                if (guardado)
                {
                    await AppUtils.MostrarSnackbar(aplicarMes ? "Disponibilidad aplicada a todo el mes" : "Disponibilidad guardada para la semana", Colors.Green, Colors.White);
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo guardar la disponibilidad", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
                _isNavigating = false;
            }
        }
    }
}