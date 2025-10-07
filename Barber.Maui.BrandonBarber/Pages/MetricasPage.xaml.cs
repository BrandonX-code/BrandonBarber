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
                List<CitaModel> citasDelMes = [];
                int mes = fechaActual.Month;
                int anio = fechaActual.Year;

                // Obtener citas del mes actual para estadísticas generales
                if (_barberiaSeleccionadaId > 0)
                {
                    var todasCitas = await _reservationService.GetReservations(fechaActual, _barberiaSeleccionadaId);
                    citasDelMes = [.. todasCitas.Where(c => c.Fecha.Month == mes && c.Fecha.Year == anio)];
                }
                else
                {
                    var todasCitas = await _reservationService.GetAllReservations();
                    citasDelMes = [.. todasCitas.Where(c => c.Fecha.Month == mes && c.Fecha.Year == anio)];
                }

                // Actualizar estadísticas generales (del mes actual)
                // Actualizar estadísticas generales (del mes actual)
                TotalCitasLabel.Text = citasDelMes.Count.ToString();

                var tasaAsistencia = citasDelMes.Count > 0
                    ? (double)citasDelMes.Count(c => c.Estado?.ToLower() == "completada") / citasDelMes.Count * 100
                    : 0;

                TasaAsistenciaLabel.Text = $"{tasaAsistencia:F1}%";

                // Los gráficos y rankings SIEMPRE muestran datos históricos (últimos 6 meses)
                await Task.WhenAll(
                    CargarGraficoAsistencia(),
                    CargarRankingBarberos(),
                    CargarClientesFrecuentes()
                );
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error al cargar métricas: {ex.Message}", Colors.Red, Colors.White);
            }
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
                SKColor.Parse("#868788"),
                SKColor.Parse("#9ebcca"),
                SKColor.Parse("#ffffff"),
                SKColor.Parse("#88a0aa"),
                SKColor.Parse("#83817e"),
                SKColor.Parse("#a5a29a")
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
                    SKColor.Parse("#ffffff"),
                    SKColor.Parse("#83817e"),
                    SKColor.Parse("#88a0aa"),
                    SKColor.Parse("#9ebcca"),
                    SKColor.Parse("#868788"),
                    SKColor.Parse("#a5a29a")
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
        //private static int DeterminarMaximoBarberos(int totalBarberos)
        //{
        //    return Math.Min(5, totalBarberos);
        //}

        //private static float DeterminarTamañoTexto(int cantidadElementos)
        //{
        //    if (cantidadElementos <= 5) return 20f;
        //    if (cantidadElementos <= 8) return 18f;
        //    return 16f;
        //}

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