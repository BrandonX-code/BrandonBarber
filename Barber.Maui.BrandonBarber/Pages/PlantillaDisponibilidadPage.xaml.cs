namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class PlantillaDisponibilidadPage : ContentPage
    {
        private readonly DisponibilidadService _disponibilidadService;
        private readonly Dictionary<string, Dictionary<string, CheckBox>> _checkboxesPorDia = new();
        private readonly string[] _diasSemana = { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };
        private readonly string[] _horarios =
        {
            "6:00 AM - 12:00 PM",
            "12:00 PM - 03:00 PM",
            "03:00 PM - 05:00 PM",
            "05:00 PM - 07:00 PM",
            "07:00 PM - 08:00 PM"
        };

        public PlantillaDisponibilidadPage(DisponibilidadService disponibilidadService)
        {
            InitializeComponent();
            _disponibilidadService = disponibilidadService;
            GenerarVista();
            _ = CargarPlantillaExistente();
        }

        private void GenerarVista()
        {
            foreach (var dia in _diasSemana)
            {
                var border = new Border
                {
                    BackgroundColor = Color.FromArgb("#90A4AE"),
                    StrokeShape = new RoundRectangle { CornerRadius = 10 },
                    Padding = 15,
                    StrokeThickness = 0,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var stack = new VerticalStackLayout { Spacing = 10 };

                // Título del día
                stack.Children.Add(new Label
                {
                    Text = dia,
                    TextColor = Colors.Black,
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold
                });

                // Crear grid de horarios
                var grid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    RowSpacing = 5
                };

                _checkboxesPorDia[dia] = new Dictionary<string, CheckBox>();

                for (int i = 0; i < _horarios.Length; i++)
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    var label = new Label
                    {
                        Text = _horarios[i],
                        TextColor = Colors.Black,
                        VerticalOptions = LayoutOptions.Center
                    };

                    var checkbox = new CheckBox
                    {
                        Color = Colors.Black,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    };

                    _checkboxesPorDia[dia][_horarios[i]] = checkbox;

                    Grid.SetRow(label, i);
                    Grid.SetColumn(label, 0);
                    Grid.SetRow(checkbox, i);
                    Grid.SetColumn(checkbox, 1);

                    grid.Children.Add(label);
                    grid.Children.Add(checkbox);
                }

                stack.Children.Add(grid);
                border.Content = stack;
                DiasContainer.Children.Add(border);
            }
        }

        private async Task CargarPlantillaExistente()
        {
            try
            {
                var barberoId = AuthService.CurrentUser?.Cedula ?? 0;
                var plantilla = await _disponibilidadService.ObtenerPlantillaSemanal(barberoId);

                if (plantilla == null || plantilla.HorariosPorDia == null)
                {
                    await DisplayAlert("Aviso", "No hay plantilla guardada.", "OK");
                    return;
                }

                foreach (var dia in _diasSemana)
                {
                    if (plantilla.HorariosPorDia.TryGetValue(dia, out var horariosDia))
                    {
                        foreach (var horario in _horarios)
                        {
                            if (horariosDia.TryGetValue(horario, out var disponible))
                            {
                                _checkboxesPorDia[dia][horario].IsChecked = disponible;
                            }
                            else
                            {
                                _checkboxesPorDia[dia][horario].IsChecked = false;
                            }
                        }
                    }
                    else
                    {
                        // Si no hay datos para el día, desmarca todos los horarios
                        foreach (var horario in _horarios)
                        {
                            _checkboxesPorDia[dia][horario].IsChecked = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo cargar la plantilla: {ex.Message}", "OK");
            }
        }
        private async void OnGuardarPlantillaClicked(object sender, EventArgs e)
        {
            try
            {
                var barberoId = AuthService.CurrentUser?.Cedula ?? 0;
                var plantilla = new PlantillaDisponibilidadModel
                {
                    BarberoId = barberoId,
                    HorariosPorDia = new Dictionary<string, Dictionary<string, bool>>()
                };

                foreach (var dia in _diasSemana)
                {
                    plantilla.HorariosPorDia[dia] = new Dictionary<string, bool>();

                    foreach (var horario in _horarios)
                    {
                        plantilla.HorariosPorDia[dia][horario] = _checkboxesPorDia[dia][horario].IsChecked;
                    }
                }

                bool resultadoPlantilla = await _disponibilidadService.GuardarPlantillaSemanal(plantilla);

                if (!resultadoPlantilla)
                {
                    await AppUtils.MostrarSnackbar("Error al guardar la plantilla", Colors.Red, Colors.White);
                    return;
                }

                // Preguntar qué rango aplicar
                string accion = await DisplayActionSheet(
                    "¿Cómo quieres aplicar tu plantilla?",
                    "Cancelar",
                    null,
                    "Solo esta semana",
                    "Las próximas 4 semanas",
                    "Todo el mes actual"
                );

                DateTime fechaInicio = DateTime.Today;
                DateTime fechaFin;

                switch (accion)
                {
                    case "Solo esta semana":
                        fechaFin = fechaInicio.AddDays(7);
                        break;
                    case "Las próximas 4 semanas":
                        fechaFin = fechaInicio.AddDays(28);
                        break;
                    case "Todo el mes actual":
                        fechaFin = new DateTime(fechaInicio.Year, fechaInicio.Month, DateTime.DaysInMonth(fechaInicio.Year, fechaInicio.Month));
                        break;
                    case "Cancelar":
                        await AppUtils.MostrarSnackbar("Plantilla guardada. Puedes aplicarla después desde 'Aplicar Plantilla al Mes'", Colors.Blue, Colors.White);
                        await Navigation.PopAsync();
                        return;
                    default:
                        await AppUtils.MostrarSnackbar("Plantilla guardada correctamente", Colors.Green, Colors.White);
                        await Navigation.PopAsync();
                        return;
                }

                // Aplicar la plantilla al rango seleccionado
                bool resultadoAplicacion = await _disponibilidadService.AplicarPlantillaARango(
                    barberoId,
                    fechaInicio,
                    fechaFin
                );

                if (resultadoAplicacion)
                {
                    await AppUtils.MostrarSnackbar($"Plantilla guardada y aplicada correctamente", Colors.Green, Colors.White);
                }
                else
                {
                    await AppUtils.MostrarSnackbar("Plantilla guardada pero hubo error al aplicarla", Colors.Orange, Colors.White);
                }

                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
            }
        }
    }
}