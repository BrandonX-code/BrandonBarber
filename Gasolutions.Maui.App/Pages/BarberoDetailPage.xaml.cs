namespace Gasolutions.Maui.App.Pages
{
    public partial class BarberoDetailPage : ContentPage
    {
        private readonly UsuarioModels _barbero;
        private readonly DisponibilidadService _disponibilidadService;
        private readonly AuthService _authService;
        private DateTime? _diaSeleccionado;
        private List<DisponibilidadModel> _disponibilidades;
        private Button _botonSeleccionado;

        public BarberoDetailPage(UsuarioModels barbero)
        {
            InitializeComponent();
            _barbero = barbero;
            _disponibilidadService = App.Current.Handler.MauiContext.Services.GetRequiredService<DisponibilidadService>();
            _authService = App.Current.Handler.MauiContext.Services.GetRequiredService<AuthService>();
            LoadBarberoData();
            ContarYMostrarVisita();
            LoadCalendario();
        }

        private void LoadBarberoData()
        {
            BarberoName.Text = _barbero.Nombre;
            BarberoImage.Source = !string.IsNullOrEmpty(_barbero.ImagenPath) ? _barbero.ImagenPath : "dotnet_bot.png";

            // Mostrar especialidades
            if (!string.IsNullOrEmpty(_barbero.Especialidades))
            {
                var especialidades = _barbero.Especialidades.Split(',').Select(e => e.Trim());
                EspecialidadesContainer.Children.Clear();

                foreach (var especialidad in especialidades)
                {
                    var frame = new Frame
                    {
                        BackgroundColor = Color.FromArgb("#4A90E2"),
                        CornerRadius = 15,
                        Padding = new Thickness(10, 5),
                        Margin = new Thickness(0, 0, 8, 8)
                    };

                    var label = new Label
                    {
                        Text = especialidad,
                        TextColor = Colors.White,
                        FontSize = 12
                    };

                    frame.Content = label;
                    EspecialidadesContainer.Children.Add(frame);
                }

                DescripcionLabel.Text = $"Barbero especializado en: {_barbero.Especialidades}";
            }
            else
            {
                DescripcionLabel.Text = "No se han especificado especialidades.";
            }
        }

        private async void LoadCalendario()
        {
            try
            {
                _disponibilidades = await _disponibilidadService.GetDisponibilidadActualPorBarbero(_barbero.Cedula);

                // Obtén solo los días con al menos un horario disponible
                var diasDisponibles = _disponibilidades?
                    .Where(d => d.HorariosDict.Any(h => h.Value))
                    .Select(d => d.Fecha.Date)
                    .ToHashSet() ?? new HashSet<DateTime>();

                GenerarCalendario(diasDisponibles);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo cargar el calendario", "OK");
            }
        }

        private void GenerarCalendario(HashSet<DateTime> diasDisponibles)
        {
            CalendarioGrid.Children.Clear();
            CalendarioGrid.RowDefinitions.Clear();
            CalendarioGrid.ColumnDefinitions.Clear();

            // Configurar columnas (7 días de la semana)
            for (int i = 0; i < 7; i++)
            {
                CalendarioGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            // Obtener el primer día del mes actual
            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            // Obtener el día de la semana del primer día (0 = Domingo, 1 = Lunes, etc.)
            var firstDayOfWeek = ((int)firstDayOfMonth.DayOfWeek + 6) % 7; // Ajustar para que Lunes sea 0

            // Calcular cuántas filas necesitamos
            var totalDays = lastDayOfMonth.Day;
            var totalCells = firstDayOfWeek + totalDays;
            var rows = (int)Math.Ceiling(totalCells / 7.0);

            // Crear las filas
            for (int i = 0; i < rows; i++)
            {
                CalendarioGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            }

            // Llenar el calendario
            for (int day = 1; day <= totalDays; day++)
            {
                var currentDate = new DateTime(today.Year, today.Month, day);
                var position = firstDayOfWeek + day - 1;
                var row = position / 7;
                var col = position % 7;

                var isAvailable = diasDisponibles.Contains(currentDate);
                var isToday = currentDate.Date == today.Date;

                var button = new Button
                {
                    Text = day.ToString(),
                    FontSize = 14,
                    CornerRadius = 20,
                    WidthRequest = 40,
                    HeightRequest = 40,
                    Margin = new Thickness(2),
                    IsEnabled = isAvailable && currentDate >= today // Solo días disponibles y futuros
                };

                // Estilo del botón según el estado
                if (!button.IsEnabled)
                {
                    // Días no disponibles o pasados
                    button.BackgroundColor = Colors.Transparent;
                    button.TextColor = Color.FromArgb("#CCCCCC");
                    button.BorderWidth = 0;
                }
                else if (isToday)
                {
                    // Día actual
                    button.BackgroundColor = Color.FromArgb("#FF6F91");
                    button.TextColor = Colors.Black;
                    button.BorderWidth = 0;
                }
                else
                {
                    // Días disponibles
                    button.BackgroundColor = Colors.Transparent;
                    button.TextColor = Colors.Black;
                    button.BorderWidth = 1;
                    button.BorderColor = Color.FromArgb("#EEEEEE");
                }

                // Evento click
                if (button.IsEnabled)
                {
                    button.Pressed += (s, e) => OnDaySelected(currentDate, button);
                }

                Grid.SetRow(button, row);
                Grid.SetColumn(button, col);
                CalendarioGrid.Children.Add(button);
            }

            // Seleccionar el día actual por defecto si está disponible
            if (diasDisponibles.Contains(today))
            {
                _diaSeleccionado = today;
                LoadHorasDisponiblesParaDia(today);
            }
            else
            {
                // Seleccionar el primer día disponible
                var primerDiaDisponible = diasDisponibles.Where(d => d >= today).OrderBy(d => d).FirstOrDefault();
                if (primerDiaDisponible != default(DateTime))
                {
                    _diaSeleccionado = primerDiaDisponible;
                    LoadHorasDisponiblesParaDia(primerDiaDisponible);
                }
            }
        }

        private void OnDaySelected(DateTime fecha, Button button)
        {
            // Restaurar el estilo del botón anteriormente seleccionado
            if (_botonSeleccionado != null && _botonSeleccionado != button)
            {
                _botonSeleccionado.BackgroundColor = Colors.Transparent;
                _botonSeleccionado.TextColor = Colors.Black;
                _botonSeleccionado.BorderWidth = 1;
                _botonSeleccionado.BorderColor = Color.FromArgb("#EEEEEE");
            }

            // Aplicar estilo al botón seleccionado
            button.BackgroundColor = Color.FromArgb("#FF6F91");
            button.TextColor = Colors.White;
            button.BorderWidth = 0;

            _botonSeleccionado = button;
            _diaSeleccionado = fecha;
            LoadHorasDisponiblesParaDia(fecha);
        }

        private void LoadHorasDisponiblesParaDia(DateTime dia)
        {
            var disponibilidadDia = _disponibilidades?.FirstOrDefault(d => d.Fecha.Date == dia.Date);
            if (disponibilidadDia != null)
            {
                var horasDisponibles = disponibilidadDia.HorariosDict
                    .Where(h => h.Value)
                    .Select(h => h.Key)
                    .ToList();

                AvailableHoursContainer.Children.Clear();
                foreach (var hora in horasDisponibles)
                {
                    var frame = new Frame
                    {
                        BackgroundColor = Color.FromArgb("#4A90E2"),
                        CornerRadius = 5,
                        Padding = new Thickness(15, 8),
                        HasShadow = false
                    };

                    var label = new Label
                    {
                        Text = hora,
                        TextColor = Colors.White,
                        FontSize = 14
                    };

                    frame.Content = label;
                    AvailableHoursContainer.Children.Add(frame);
                }

                NoAvailabilityLabel.IsVisible = !horasDisponibles.Any();
                AvailableHoursContainer.IsVisible = horasDisponibles.Any();
            }
        }

        private async void ContarYMostrarVisita()
        {
            var barberoActualizado = await _authService.GetUserByCedula(_barbero.Cedula);
            if (barberoActualizado != null)
            {
                BarberoVisitasLabel.Text = $"Visitas: {barberoActualizado.Visitas}";
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnMakeAppointmentClicked(object sender, EventArgs e)
        {
            var reservationService = App.Current.Handler.MauiContext.Services.GetRequiredService<ReservationService>();
            var authService = App.Current.Handler.MauiContext.Services.GetRequiredService<AuthService>();
            await Navigation.PushAsync(new MainPage(reservationService, authService));
        }
    }
}