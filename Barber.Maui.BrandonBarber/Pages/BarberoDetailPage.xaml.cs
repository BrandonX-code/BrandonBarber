namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class BarberoDetailPage : ContentPage
    {
        private readonly UsuarioModels _barbero;
        private readonly DisponibilidadService _disponibilidadService;
        private readonly AuthService _authService;
        private readonly ReservationService _reservationService;
        private DateTime? _diaSeleccionado;
        private List<DisponibilidadModel>? _disponibilidades;
        private HashSet<DateTime> _diasDisponibles = new();

        public BarberoDetailPage(UsuarioModels barbero)
        {
            InitializeComponent();
            _barbero = barbero;
            _disponibilidadService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<DisponibilidadService>();
            _authService = App.Current.Handler.MauiContext.Services.GetRequiredService<AuthService>();
            _reservationService = App.Current.Handler.MauiContext.Services.GetRequiredService<ReservationService>();
            LoadBarberoData();
            ContarYMostrarVisita();
            LoadCalendario();
            _ = CargarPromedioCalificacion();
            WeakReferenceMessenger.Default.Register<CalificacionEnviadaMessage>(this, async (r, m) =>
            {
                if (m.Value == _barbero.Cedula)
                    await CargarPromedioCalificacion();
            });
        }

        private void LoadBarberoData()
        {
            BarberoName.Text = _barbero.Nombre;
            BarberoImage.Source = !string.IsNullOrEmpty(_barbero.ImagenPath) ? _barbero.ImagenPath : "dotnet_bot.png";

            if (!string.IsNullOrEmpty(_barbero.Especialidades))
            {
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

                if (_disponibilidades == null || !_disponibilidades.Any())
                {
                    // No hay disponibilidad gestionada
                    _diasDisponibles = new HashSet<DateTime>();
                    GenerarCalendario(_diasDisponibles);
                    MostrarMensajeSinDisponibilidad();
                    return;
                }

                // Obtén solo los días con al menos un horario disponible
                _diasDisponibles = _disponibilidades
                    .Where(d => d.HorariosDict.Any(h => h.Value))
                    .Select(d => d.Fecha.Date)
                    .ToHashSet();

                GenerarCalendario(_diasDisponibles);
            }
            catch (Exception)
            {
                await DisplayAlert("Error", "No se pudo cargar el calendario", "OK");
            }
        }

        private void MostrarMensajeSinDisponibilidad()
        {
            NoAvailabilityLabel.Text = "El barbero no ha configurado su disponibilidad";
            NoAvailabilityLabel.IsVisible = true;
            AvailableHoursContainer.IsVisible = false;
            _diaSeleccionado = null;
        }

        private void GenerarCalendario(HashSet<DateTime> diasDisponibles)
        {
            CalendarioGrid.Children.Clear();
            CalendarioGrid.RowDefinitions.Clear();
            CalendarioGrid.ColumnDefinitions.Clear();

            // Configurar columnas (7 días de la semana) con espaciado uniforme
            for (int i = 0; i < 7; i++)
            {
                CalendarioGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            // Ajustar para que Lunes sea el primer día (0=Lunes, 6=Domingo)
            var firstDayOfWeek = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;

            var totalDays = lastDayOfMonth.Day;
            var totalCells = firstDayOfWeek + totalDays;
            var rows = (int)Math.Ceiling(totalCells / 7.0);

            // Crear las filas con altura ajustable para mejor distribución
            for (int i = 0; i < rows; i++)
            {
                CalendarioGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(45, GridUnitType.Absolute) });
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
                var isSelected = _diaSeleccionado.HasValue && _diaSeleccionado.Value.Date == currentDate.Date;

                // Crear un Frame contenedor con mejor espaciado
                var border = new Border
                {
                    Stroke = Colors.Transparent,
                    BackgroundColor = Colors.Transparent,
                    Padding = new Thickness(3),
                    Margin = new Thickness(2),
                    StrokeShape = new RoundRectangle
                    {
                        CornerRadius = 5
                    }
                };

                var button = new Button
                {
                    Text = day.ToString(),
                    FontSize = 13,
                    CornerRadius = 8,
                    WidthRequest = 35,
                    HeightRequest = 32,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Padding = new Thickness(0),
                    FontAttributes = FontAttributes.Bold
                };

                // Configurar el botón según su estado
                if (currentDate < today)
                {
                    // Días pasados - deshabilitados
                    button.BackgroundColor = Colors.Transparent;
                    button.TextColor = Color.FromArgb("#666666");
                    button.BorderWidth = 0;
                    button.FontAttributes = FontAttributes.None;
                    button.IsEnabled = false;
                    button.InputTransparent = true;
                }
                else if (isSelected)
                {
                    // Día seleccionado
                    button.BackgroundColor = Color.FromArgb("#FF6F91");
                    button.TextColor = Colors.Black;
                    button.BorderWidth = 0;
                    button.FontAttributes = FontAttributes.Bold;
                    button.IsEnabled = true;
                    button.InputTransparent = false;
                }
                else if (isToday && isAvailable)
                {
                    // Hoy con disponibilidad
                    button.BackgroundColor = Color.FromArgb("#0e2a36");
                    button.TextColor = Colors.White;
                    button.BorderWidth = 0;
                    button.FontAttributes = FontAttributes.Bold;
                    button.IsEnabled = true;
                    button.InputTransparent = false;
                }
                else if (isAvailable)
                {
                    // Días futuros con disponibilidad
                    button.BackgroundColor = Colors.Transparent;
                    button.TextColor = Colors.Black;
                    button.BorderWidth = 1;
                    button.BorderColor = Colors.Transparent;
                    button.FontAttributes = FontAttributes.None;
                    button.IsEnabled = true;
                    button.InputTransparent = false;
                }
                else
                {
                    // Días futuros sin disponibilidad - también seleccionables
                    button.BackgroundColor = Colors.Transparent;
                    button.TextColor = Colors.Black;
                    button.BorderColor = Color.FromArgb("#CCCCCC");
                    button.FontAttributes = FontAttributes.None;
                    button.IsEnabled = true;
                    button.InputTransparent = false;
                }

                // Agregar evento click a todos los días futuros (incluyendo hoy)
                if (currentDate >= today)
                {
                    var dateToCapture = currentDate;
                    button.Clicked += (s, e) => OnDaySelected(dateToCapture);
                }

                border.Content = button;
                Grid.SetRow(border, row);
                Grid.SetColumn(border, col);
                CalendarioGrid.Children.Add(border);
            }

            // Mostrar horarios del día seleccionado, o del día actual por defecto si está disponible
            if (_diaSeleccionado.HasValue && diasDisponibles.Contains(_diaSeleccionado.Value))
            {
                LoadHorasDisponiblesParaDia(_diaSeleccionado.Value);
            }
            else if (diasDisponibles.Contains(today))
            {
                _diaSeleccionado = today;
                LoadHorasDisponiblesParaDia(today);
            }
            else
            {
                var primerDiaDisponible = diasDisponibles.Where(d => d >= today).OrderBy(d => d).FirstOrDefault();
                if (primerDiaDisponible != default)
                {
                    _diaSeleccionado = primerDiaDisponible;
                    LoadHorasDisponiblesParaDia(primerDiaDisponible);
                }
                else if (!diasDisponibles.Any())
                {
                    MostrarMensajeSinDisponibilidad();
                }
            }
        }

        private void OnDaySelected(DateTime selectedDate)
        {
            _diaSeleccionado = selectedDate;
            // Regenerar el calendario para actualizar el estado visual
            GenerarCalendario(_diasDisponibles);
            // Cargar las horas disponibles para el día seleccionado
            LoadHorasDisponiblesParaDia(selectedDate);
        }

        private void LoadHorasDisponiblesParaDia(DateTime dia)
        {
            var disponibilidadDia = _disponibilidades?.FirstOrDefault(d => d.Fecha.Date == dia.Date);

            if (disponibilidadDia == null)
            {
                // El barbero no ha gestionado disponibilidad para este día
                NoAvailabilityLabel.Text = "El barbero no ha configurado su disponibilidad para este día";
                NoAvailabilityLabel.IsVisible = true;
                AvailableHoursContainer.IsVisible = false;
                return;
            }

            var horasDisponibles = disponibilidadDia.HorariosDict
                .Where(h => h.Value)
                .Select(h => h.Key)
                .ToList();

            AvailableHoursContainer.Children.Clear();

            if (horasDisponibles.Count == 0)
            {
                NoAvailabilityLabel.Text = "No hay horarios disponibles para este día";
                NoAvailabilityLabel.IsVisible = true;
                AvailableHoursContainer.IsVisible = false;
                return;
            }

            foreach (var hora in horasDisponibles)
            {
                var border = new Border
                {
                    BackgroundColor = Color.FromArgb("#90A4AE"),
                    Padding = new Thickness(15, 8),
                    HeightRequest = 40,
                    Stroke = Colors.Transparent,
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle
                    {
                        CornerRadius = 5
                    }
                };

                var label = new Label
                {
                    Text = hora,
                    TextColor = Colors.Black,
                    FontSize = 14
                };

                border.Content = label;
                AvailableHoursContainer.Children.Add(border);
            }

            NoAvailabilityLabel.IsVisible = false;
            AvailableHoursContainer.IsVisible = true;
        }

        private async void ContarYMostrarVisita()
        {
            try
            {
                var barberoCedula = AuthService.CurrentUser!.Cedula;
                // Obtener todas las citas
                var todasLasCitas = await _reservationService.GetReservationsById(barberoCedula);

                // Contar las citas donde el barbero sea el actual
                int visitas = todasLasCitas.Count(c => c.BarberoId == _barbero.Cedula);

                BarberoVisitasLabel.Text = $"Visitas: {visitas}";
            }
            catch (Exception)
            {
                BarberoVisitasLabel.Text = "Visitas: 0";
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnMakeAppointmentClicked(object sender, EventArgs e)
        {
            var reservationService = App.Current!.Handler.MauiContext!.Services.GetRequiredService<ReservationService>();
            var authService = App.Current.Handler.MauiContext.Services.GetRequiredService<AuthService>();
            // Navega a ReservaCita y pasa el barbero actual como preseleccionado
            await Navigation.PushAsync(new MainPage(reservationService, authService, _barbero));
        }

        private void ActualizarCalificacionVisual()
        {
            var calificacion = _barbero.CalificacionPromedio;
            var estrellas = "";
            for (int i = 1; i <= 5; i++)
            {
                estrellas += i <= calificacion ? "★" : "☆";
            }
            CalificacionLabel.Text = estrellas;
            PromedioLabel.Text = $"({calificacion:F1})";

            // Solo mostrar botón de calificar si el usuario actual es cliente
            CalificarButton.IsVisible = AuthService.CurrentUser?.Rol?.ToLower() == "cliente";
        }

        private async void OnCalificarClicked(object sender, EventArgs e)
        {
            try
            {
                // Validar que el usuario actual sea válido y sea cliente
                if (AuthService.CurrentUser == null)
                {
                    await DisplayAlert("Error", "Usuario no autenticado", "OK");
                    return;
                }

                if (AuthService.CurrentUser.Rol?.ToLower() != "cliente")
                {
                    await DisplayAlert("Error", "Solo los clientes pueden calificar barberos", "OK");
                    return;
                }

                // Validar que el barbero sea válido
                if (_barbero == null)
                {
                    await DisplayAlert("Error", "Error al cargar información del barbero", "OK");
                    return;
                }

                // Navegar a la página de calificación
                await Navigation.PushAsync(new CalificarBarberoPage(_barbero));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo abrir la página de calificación: {ex.Message}", "OK");
            }
        }

        private async Task CargarPromedioCalificacion()
        {
            var calificacionService = Application.Current!.Handler.MauiContext!.Services.GetService<CalificacionService>()!;
            var (promedio, _) = await calificacionService.ObtenerPromedioAsync(_barbero.Cedula);

            // Actualiza el modelo y la UI
            _barbero.CalificacionPromedio = promedio;
            ActualizarCalificacionVisual();
        }
    }
}