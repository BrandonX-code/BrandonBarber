using CommunityToolkit.Mvvm.Messaging;
using Gasolutions.Maui.App.Messenger;
using Microsoft.Maui.Controls.Shapes;

namespace Gasolutions.Maui.App.Pages
{
    public partial class BarberoDetailPage : ContentPage
    {
        private readonly UsuarioModels _barbero;
        private readonly DisponibilidadService _disponibilidadService;
        private readonly AuthService _authService;
        private readonly ReservationService _reservationService;
        private DateTime? _diaSeleccionado;
        private List<DisponibilidadModel>? _disponibilidades;
        
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
            _= CargarPromedioCalificacion();
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

                // Obtén solo los días con al menos un horario disponible
                var diasDisponibles = _disponibilidades?
                    .Where(d => d.HorariosDict.Any(h => h.Value))
                    .Select(d => d.Fecha.Date)
                    .ToHashSet() ?? [];

                GenerarCalendario(diasDisponibles);
            }
            catch (Exception)
            {
                await DisplayAlert("Error", "No se pudo cargar el calendario", "OK");
            }
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

                // Crear un Frame contenedor con mejor espaciado
                var border = new Border
                {
                    Stroke = Colors.Transparent,
                    BackgroundColor = Colors.Transparent,
                    Padding = new Thickness(3),
                    Margin = new Thickness(2), 
                    StrokeShape = new RoundRectangle()
                };
                border.StrokeShape = new RoundRectangle
                {
                    CornerRadius = 5
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
                    IsEnabled = false,
                    InputTransparent = true,
                    Padding = new Thickness(0),
                    FontAttributes = FontAttributes.Bold
                };

                if (!isAvailable || currentDate < today)
                {
                    button.BackgroundColor = Colors.Transparent;
                    button.TextColor = Color.FromArgb("#000000");
                    button.BorderWidth = 0;
                    button.FontAttributes = FontAttributes.None;
                }
                else if (isToday)
                {
                    button.BackgroundColor = Color.FromArgb("#0e2a36");
                    button.TextColor = Colors.White;
                    button.BorderWidth = 0;
                    button.FontAttributes = FontAttributes.Bold;
                }
                else
                {
                    button.BackgroundColor = Colors.Transparent;
                    button.TextColor = Colors.Black;
                    button.BorderWidth = 1;
                    button.BorderColor = Color.FromArgb("#EEEEEE");
                    button.FontAttributes = FontAttributes.None;
                }

                border.Content = button;
                Grid.SetRow(border, row);
                Grid.SetColumn(border, col);
                CalendarioGrid.Children.Add(border);
            }

            // Mostrar horarios del día actual por defecto si está disponible
            if (diasDisponibles.Contains(today))
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
            }
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

                NoAvailabilityLabel.IsVisible = horasDisponibles.Count == 0;
                AvailableHoursContainer.IsVisible = horasDisponibles.Count != 0;
            }
        }

        private async void ContarYMostrarVisita()
        {
            try
            {
                var barberoCedula = AuthService.CurrentUser.Cedula;
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
            await Navigation.PushAsync(new MainPage(reservationService, authService));
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
            await Navigation.PushAsync(new CalificarBarberoPage(_barbero));
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