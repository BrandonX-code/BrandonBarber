using Microcharts;
using SkiaSharp;
using Entry = Microcharts.ChartEntry;
using Command = Microsoft.Maui.Controls.Command;
using Barber.Maui.BrandonBarber.Models;
using Barber.Maui.BrandonBarber.Services;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class MetricasPage : ContentPage
    {
        private readonly ReservationService _reservationService;
        private readonly AuthService _authService;
        private readonly BarberiaService? _barberiaService;
        private List<Barberia>? _barberias;
        private int _barberiaSeleccionadaId; // ID de la barbería seleccionada
        public Command RefreshCommand { get; }

        public MetricasPage(ReservationService reservationService, AuthService authService)
        {
            InitializeComponent();
            _reservationService = reservationService;
            _authService = authService;
            _barberiaService = Application.Current!.Handler.MauiContext!.Services.GetService<BarberiaService>();
            RefreshCommand = new Command(async () => await RefreshMetricas());
            BindingContext = this;
            ChartTypePicker.SelectedIndex = 0;
            RankingChartTypePicker.SelectedIndex = 0;
            AsistenciaChartTypePicker.SelectedIndex = 0;
            // Cargar barberías primero, luego métricas
            _ = CargarBarberias();
        }

        private async Task CargarBarberias()
        {
            try
            {
                long idAdministrador = AuthService.CurrentUser!.Cedula;
                _barberias = await _barberiaService!.GetBarberiasByAdministradorAsync(idAdministrador);

                BarberiaPicker.ItemsSource = _barberias;
                BarberiaPicker.ItemDisplayBinding = new Binding("Nombre");
                PickerSection.IsVisible = _barberias.Count > 0;

                if (_barberias.Count > 0)
                {
                    BarberiaPicker.SelectedIndex = 0; // Esto disparará automáticamente el evento
                }
                else
                {
                    // Si no hay barberías, mostrar métricas generales
                    _barberiaSeleccionadaId = 0;
                    await CargarMetricas();
                }
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error al cargar barberías: {ex.Message}", Colors.Red, Colors.White);
                // Si falla cargar barberías, mostrar métricas generales
                _barberiaSeleccionadaId = 0;
                await CargarMetricas();
            }
        }

        private async void BarberiaPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            var picker = (Picker)sender;
            int selectedIndex = picker.SelectedIndex;
            if (selectedIndex != -1)
            {
                var barberiaSeleccionada = (Barberia)picker.SelectedItem;
                _barberiaSeleccionadaId = barberiaSeleccionada.Idbarberia;
                await CargarMetricas();
            }
        }

        private async Task RefreshMetricas()
        {
            if (metricsRefreshView.IsRefreshing)
            {
                await CargarMetricas();
                metricsRefreshView.IsRefreshing = false;
            }
        }

        private async Task CargarMetricas()
        {
            try
            {
                var fechaActual = DateTime.Now;
                int mes = fechaActual.Month;
                int anio = fechaActual.Year;

                // Obtener todas las citas históricas
                List<CitaModel> todasLasCitas;
                if (_barberiaSeleccionadaId > 0)
                {
                    todasLasCitas = await _reservationService.GetReservationsByBarberia(_barberiaSeleccionadaId);
                }
                else
                {
                    todasLasCitas = await _reservationService.GetAllReservationsHistorical();
                }

                // Filtrar solo las citas de HOY para "Total Citas Hoy"
                var citasHoy = todasLasCitas.Where(c =>
                    c.Fecha.Date == fechaActual.Date).ToList();

                // Filtrar las del mes actual para la tasa de asistencia
                var citasDelMes = todasLasCitas.Where(c =>
                    c.Fecha.Month == mes && c.Fecha.Year == anio).ToList();

                // Actualizar estadísticas generales
                TotalCitasLabel.Text = citasHoy.Count.ToString(); // ← CITAS DE HOY

                var tasaAsistencia = citasDelMes.Count > 0
                    ? (double)citasDelMes.Count(c => c.Estado?.ToLower() == "completada") / citasDelMes.Count * 100
                    : 0;

                TasaAsistenciaLabel.Text = $"{tasaAsistencia:F1}%"; // ← TASA DEL MES

                // Los gráficos y rankings SIEMPRE muestran datos históricos (últimos 6 meses)
                await Task.WhenAll(
                    CargarGraficoAsistencia(),
                    CargarGraficoTasaAsistencia(),
                    CargarRankingBarberos(),
                    CargarClientesFrecuentes()
                );
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error al cargar métricas: {ex.Message}", Colors.Red, Colors.White);
            }
        }

        private void OnAsistenciaChartTypeChanged(object sender, EventArgs e)
        {
            CargarGraficoTasaAsistencia().ConfigureAwait(false);
        }

        private async Task CargarGraficoTasaAsistencia()
        {
            try
            {
                var entries = await ObtenerDatosTasaAsistencia();

                Chart chart;
                if (AsistenciaChartTypePicker.SelectedIndex == 0)
                {
                    chart = new BarChart
                    {
                        Entries = entries,
                        LabelTextSize = 40,
                        Margin = 40,
                        LabelOrientation = Orientation.Horizontal,
                        ValueLabelOrientation = Orientation.Horizontal,
                        BackgroundColor = SKColor.Parse("#0E2A36"),
                        MaxValue = 100 // Para porcentajes
                    };
                }
                else
                {
                    chart = new LineChart
                    {
                        Entries = entries,
                        LabelTextSize = 40,
                        Margin = 40,
                        LabelOrientation = Orientation.Horizontal,
                        ValueLabelOrientation = Orientation.Horizontal,
                        BackgroundColor = SKColor.Parse("#0E2A36"),
                        MaxValue = 100 // Para porcentajes
                    };
                }

                TasaAsistenciaChart.Chart = chart;
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error al cargar gráfico de tasa: {ex.Message}", Colors.Red, Colors.White);
            }
        }

        private async Task<List<ChartEntry>> ObtenerDatosTasaAsistencia()
        {
            var entries = new List<ChartEntry>();
            var fechaActual = DateTime.Now;
            var colores = new[]
            {
                SKColor.Parse("#4CAF50"), // Verde para alta asistencia
                SKColor.Parse("#8BC34A"),
                SKColor.Parse("#FFC107"), // Amarillo para media
                SKColor.Parse("#FF9800"),
                SKColor.Parse("#FF5722"), // Rojo para baja
                SKColor.Parse("#F44336")
            };

            // Obtener todas las citas históricas de una vez
            List<CitaModel> todasLasCitas;
            if (_barberiaSeleccionadaId > 0)
            {
                todasLasCitas = await _reservationService.GetReservationsByBarberia(_barberiaSeleccionadaId);
            }
            else
            {
                todasLasCitas = await _reservationService.GetAllReservationsHistorical();
            }

            for (int i = 5; i >= 0; i--)
            {
                var fecha = fechaActual.AddMonths(-i);

                // Filtrar las citas del mes específico
                var citasDelMes = todasLasCitas.Where(c =>
                    c.Fecha.Year == fecha.Year &&
                    c.Fecha.Month == fecha.Month).ToList();

                var tasaAsistencia = citasDelMes.Count > 0
                    ? (float)((double)citasDelMes.Count(c => c.Estado?.ToLower() == "completada") / citasDelMes.Count * 100)
                    : 0f;

                // Elegir color según la tasa
                SKColor color;
                // Color según la tasa - ya está bien, solo ajusta los colores:
                if (tasaAsistencia >= 80) color = SKColor.Parse("#2E7D32"); // Verde oscuro
                else if (tasaAsistencia >= 60) color = SKColor.Parse("#66BB6A"); // Verde claro
                else if (tasaAsistencia >= 40) color = SKColor.Parse("#FFA726"); // Naranja
                else if (tasaAsistencia >= 20) color = SKColor.Parse("#FF7043"); // Naranja rojizo
                else color = SKColor.Parse("#E53935"); // Rojo

                entries.Add(new ChartEntry(tasaAsistencia)
                {
                    Label = fecha.ToString("MMM"),
                    ValueLabel = $"{tasaAsistencia:F1}%",
                    Color = color,
                    TextColor = SKColor.Parse("#ffffff"),
                    ValueLabelColor = SKColor.Parse("#ffffff")
                });
            }

            return entries;
        }

        private void OnChartTypeChanged(object sender, EventArgs e)
        {
            CargarGraficoAsistencia().ConfigureAwait(false);
        }

        private void OnRankingChartTypeChanged(object sender, EventArgs e)
        {
            CargarRankingBarberos().ConfigureAwait(false);
        }

        private async Task CargarGraficoAsistencia()
        {
            try
            {
                var entries = await ObtenerDatosAsistencia();

                Chart chart;
                if (ChartTypePicker.SelectedIndex == 0)
                {
                    chart = new BarChart
                    {
                        Entries = entries,
                        LabelTextSize = 40,
                        Margin = 40,
                        LabelOrientation = Orientation.Horizontal,
                        ValueLabelOrientation = Orientation.Horizontal,
                        BackgroundColor = SKColor.Parse("#0E2A36")
                    };
                }
                else
                {
                    chart = new LineChart
                    {
                        Entries = entries,
                        LabelTextSize = 40,
                        Margin = 40,
                        LabelOrientation = Orientation.Horizontal,
                        ValueLabelOrientation = Orientation.Horizontal,
                        BackgroundColor = SKColor.Parse("#0E2A36")
                    };
                }

                AsistenciaChart.Chart = chart;
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error al cargar gráfico: {ex.Message}", Colors.Red, Colors.White);
            }
        }

        private async Task<List<ChartEntry>> ObtenerDatosAsistencia()
        {
            var entries = new List<ChartEntry>();
            var fechaActual = DateTime.Now;
            var colores = new[]
            {
                SKColor.Parse("#1E3A5F"), // Azul oscuro profundo
                SKColor.Parse("#2E5C8A"), // Azul medio
                SKColor.Parse("#4A90C8"), // Azul cielo
                SKColor.Parse("#7FB3D5"), // Azul claro
                SKColor.Parse("#B0D4E8"), // Azul pastel
                SKColor.Parse("#D4E8F5")  // Azul muy claro
            };

            // Obtener todas las citas históricas de una vez
            List<CitaModel> todasLasCitas;
            if (_barberiaSeleccionadaId > 0)
            {
                todasLasCitas = await _reservationService.GetReservationsByBarberia(_barberiaSeleccionadaId);
            }
            else
            {
                todasLasCitas = await _reservationService.GetAllReservationsHistorical();
            }

            for (int i = 5; i >= 0; i--)
            {
                var fecha = fechaActual.AddMonths(-i);

                // Filtrar las citas del mes específico
                var citasDelMes = todasLasCitas.Where(c =>
                    c.Fecha.Year == fecha.Year &&
                    c.Fecha.Month == fecha.Month).ToList();

                entries.Add(new ChartEntry(citasDelMes.Count)
                {
                    Label = fecha.ToString("MMM"),
                    ValueLabel = citasDelMes.Count.ToString(),
                    Color = colores[5 - i],
                    TextColor = SKColor.Parse("#ffffff"),
                    ValueLabelColor = SKColor.Parse("#ffffff")
                });
            }

            return entries;
        }
        private async Task CargarRankingBarberos()
        {
            try
            {
                // Obtener todas las citas históricas
                List<CitaModel> todasLasCitas;
                if (_barberiaSeleccionadaId > 0)
                {
                    todasLasCitas = await _reservationService.GetReservationsByBarberia(_barberiaSeleccionadaId);
                }
                else
                {
                    todasLasCitas = await _reservationService.GetAllReservationsHistorical();
                }

                // Filtrar por los últimos 6 meses para el ranking
                var fechaLimite = DateTime.Now.AddMonths(-6);
                var citasUltimos6Meses = todasLasCitas.Where(c => c.Fecha >= fechaLimite).ToList();

                var ranking = citasUltimos6Meses
                    .Where(c => c.BarberoId > 0)
                    .GroupBy(c => c.BarberoId)
                    .Select(g => new { BarberoId = g.Key, Total = g.Count() })
                    .OrderByDescending(x => x.Total)
                    .ToList();

                var entries = new List<ChartEntry>();
                var colores = new[]
                {
                    SKColor.Parse("#9BB9D4"), // Gris azulado claro
                    SKColor.Parse("#546E7A"), // Gris azulado
                    SKColor.Parse("#78909C"), // Gris azulado claro
                    SKColor.Parse("#90A4AE"), // Gris plateado
                    SKColor.Parse("#B0BEC5"), // Gris perla
                    SKColor.Parse("#CFD8DC")  // Gris muy claro
                };

                int maxBarberosAMostrar = 5; // Mostrar máximo 5 barberos
                int colorIndex = 0;
                int barberosAgregados = 0;

                foreach (var item in ranking.Take(maxBarberosAMostrar))
                {
                    var barbero = await _authService.GetUserByCedula(item.BarberoId);
                    if (barbero != null)
                    {
                        entries.Add(new ChartEntry(item.Total)
                        {
                            Label = TruncateLabel(barbero.Nombre!, 10),
                            ValueLabel = item.Total.ToString(),
                            Color = colores[colorIndex++ % colores.Length],
                            TextColor = SKColor.Parse("#ffffff"),
                            ValueLabelColor = SKColor.Parse("#ffffff")
                        });
                        barberosAgregados++;
                    }
                }

                if (ranking.Count > maxBarberosAMostrar)
                {
                    var otrosTotal = ranking.Skip(maxBarberosAMostrar).Sum(x => x.Total);
                    entries.Add(new ChartEntry(otrosTotal)
                    {
                        Label = $"Otros ({ranking.Count - maxBarberosAMostrar})",
                        ValueLabel = otrosTotal.ToString(),
                        Color = SKColor.Parse("#666666"),
                        TextColor = SKColor.Parse("#ffffff"),
                        ValueLabelColor = SKColor.Parse("#ffffff")
                    });
                }

                int minEntries = Math.Min(6, maxBarberosAMostrar);
                while (entries.Count < minEntries && ranking.Count < 3)
                {
                    entries.Add(new ChartEntry(0)
                    {
                        Label = "",
                        ValueLabel = "0",
                        Color = colores[colorIndex++ % colores.Length],
                        TextColor = SKColor.Parse("#ffffff"),
                        ValueLabelColor = SKColor.Parse("#ffffff")
                    });
                }

                if (ranking.Count == 0)
                {
                    var mensaje = _barberiaSeleccionadaId > 0
                        ? "No hay datos de barberos para la barbería seleccionada."
                        : "No hay datos de barberos para mostrar en el ranking.";
                    await AppUtils.MostrarSnackbar(mensaje, Colors.Orange, Colors.White);
                }

                Chart chart;
                if (RankingChartTypePicker.SelectedIndex == 0)
                {
                    chart = new BarChart
                    {
                        Entries = entries,
                        LabelTextSize = 40,
                        Margin = 40,
                        LabelOrientation = Orientation.Horizontal,
                        ValueLabelOrientation = Orientation.Horizontal,
                        BackgroundColor = SKColor.Parse("#0E2A36")
                    };
                }
                else
                {
                    chart = new LineChart
                    {
                        Entries = entries,
                        LabelTextSize = 40,
                        Margin = 40,
                        LabelOrientation = Orientation.Horizontal,
                        ValueLabelOrientation = Orientation.Horizontal,
                        BackgroundColor = SKColor.Parse("#0E2A36")
                    };
                }

                RankingBarberosChart.Chart = chart;

                if (ranking.Count > maxBarberosAMostrar)
                {
                    await AppUtils.MostrarSnackbar($"Mostrando top {maxBarberosAMostrar} de {ranking.Count} barberos", Colors.Green, Colors.White);
                }
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error al cargar ranking: {ex.Message}", Colors.Red, Colors.White);
            }
        }

        private static string TruncateLabel(string nombre, int maxLength)
        {
            if (string.IsNullOrEmpty(nombre)) return "";
            return nombre.Length <= maxLength ? nombre : nombre[..(maxLength - 2)] + "..";
        }

        private async Task CargarClientesFrecuentes()
        {
            try
            {
                // Obtener todas las citas históricas
                List<CitaModel> todasLasCitas;
                if (_barberiaSeleccionadaId > 0)
                {
                    todasLasCitas = await _reservationService.GetReservationsByBarberia(_barberiaSeleccionadaId);
                }
                else
                {
                    todasLasCitas = await _reservationService.GetAllReservationsHistorical();
                }

                // Filtrar por los últimos 6 meses
                var fechaLimite = DateTime.Now.AddMonths(-6);
                var citasUltimos6Meses = todasLasCitas.Where(c => c.Fecha >= fechaLimite).ToList();

                var clientesFrecuentes = citasUltimos6Meses
                    .Where(c => c.Cedula > 0)
                    .GroupBy(c => c.Cedula)
                    .Select(g => new { Cedula = g.Key, Visitas = g.Count() })
                    .OrderByDescending(x => x.Visitas)
                    .Take(5)
                    .ToList();

                var clientesDetallados = new List<ClienteFrecuenteExtendido>();
                int position = 1;
                foreach (var cliente in clientesFrecuentes)
                {
                    var usuario = await _authService.GetUserByCedula(cliente.Cedula);
                    if (usuario != null)
                    {
                        clientesDetallados.Add(new ClienteFrecuenteExtendido
                        {
                            Position = $"#{position++}",
                            Nombre = usuario.Nombre,
                            Visitas = cliente.Visitas
                        });
                    }
                }

                if (clientesDetallados.Count == 0)
                {
                    var mensaje = _barberiaSeleccionadaId > 0
                        ? "No hay datos de clientes para la barbería seleccionada."
                        : "No hay datos de clientes para mostrar en el ranking.";
                    await AppUtils.MostrarSnackbar(mensaje, Colors.Orange, Colors.White);
                }

                ClientesFrecuentesCollection.ItemsSource = clientesDetallados;
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error al cargar clientes frecuentes: {ex.Message}", Colors.Red, Colors.White);
            }
        }
    }

    public class ClienteFrecuenteExtendido : ClienteFrecuente
    {
        public string? Position { get; set; }
    }

    public class ClienteFrecuente
    {
        public string? Nombre { get; set; }
        public int Visitas { get; set; }
    }
}