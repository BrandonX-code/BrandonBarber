using Microcharts;
using SkiaSharp;
using Entry = Microcharts.ChartEntry;
using Command = Microsoft.Maui.Controls.Command;

namespace Gasolutions.Maui.App.Pages
{
    public partial class MetricasPage : ContentPage
    {
        private readonly ReservationService _reservationService;
        private readonly AuthService _authService;
        public Command RefreshCommand { get; }

        public MetricasPage(ReservationService reservationService, AuthService authService)
        {
            InitializeComponent();
            _reservationService = reservationService;
            _authService = authService;
            RefreshCommand = new Command(async () => await RefreshMetricas());
            BindingContext = this;
            ChartTypePicker.SelectedIndex = 0;
            RankingChartTypePicker.SelectedIndex = 0;
            CargarMetricas();
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
                var fechaActual = DateTime.Now; // Agregar fecha actual como parámetro
                var todasLasCitas = await _reservationService.GetReservations(fechaActual);

                // Actualizar estadísticas generales
                TotalCitasLabel.Text = todasLasCitas.Count.ToString();
                var tasaAsistencia = todasLasCitas.Count > 0
                    ? (double)todasLasCitas.Count(c => c.Estado == "Completada") / todasLasCitas.Count * 100
                    : 0;
                TasaAsistenciaLabel.Text = $"{tasaAsistencia:F1}%";

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

        //private async Task CargarGraficoCancelaciones()
        //{
        //    try
        //    {
        //        var entries = new List<ChartEntry>();
        //        var fechaActual = DateTime.Now;
        //        var colores = new[]
        //        {
        //            SKColor.Parse("#FFD700"),
        //            SKColor.Parse("#c0aa4f"),
        //            SKColor.Parse("#DAA520"),
        //            SKColor.Parse("#B8860B"),
        //            SKColor.Parse("#CD853F"),
        //            SKColor.Parse("#D2691E")
        //        };

        //        for (int i = 5; i >= 0; i--)
        //        {
        //            var fecha = fechaActual.AddMonths(-i);
        //            var todasLasCitas = await _reservationService.GetReservations(fecha);
        //            var citasCanceladas = todasLasCitas.Count(c => c.Estado == "Cancelada");

        //            entries.Add(new ChartEntry(citasCanceladas)
        //            {
        //                Label = fecha.ToString("MMM"),
        //                ValueLabel = citasCanceladas.ToString(),
        //                Color = colores[5 - i],
        //                TextColor = SKColor.Parse("#232323"),
        //                ValueLabelColor = SKColor.Parse("#232323")
        //            });
        //        }

        //        CancelacionesChart.Chart = new BarChart
        //        {
        //            Entries = entries,
        //            LabelTextSize = 40,
        //            LabelOrientation = Orientation.Horizontal,
        //            ValueLabelOrientation = Orientation.Horizontal,
        //            BackgroundColor = SKColor.Parse("#fffbe6")
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        await AppUtils.MostrarSnackbar($"Error al cargar gráfico de cancelaciones: {ex.Message}", Colors.Red, Colors.White);
        //    }
        //}

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
                        Margin = 50,
                        LabelOrientation = Orientation.Horizontal,
                        ValueLabelOrientation = Orientation.Horizontal,
                        BackgroundColor = SKColor.Parse("#fffbe6")
                    };
                }
                else
                {
                    chart = new LineChart
                    {
                        Entries = entries,
                        LabelTextSize = 40,
                        Margin = 50,
                        LabelOrientation = Orientation.Horizontal,
                        ValueLabelOrientation = Orientation.Horizontal,
                        BackgroundColor = SKColor.Parse("#fffbe6")
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
                SKColor.Parse("#FFD700"),
                SKColor.Parse("#c0aa4f"),
                SKColor.Parse("#DAA520"),
                SKColor.Parse("#B8860B"),
                SKColor.Parse("#CD853F"),
                SKColor.Parse("#D2691E")
            };

            for (int i = 5; i >= 0; i--)
            {
                var fecha = fechaActual.AddMonths(-i);
                var citas = await _reservationService.GetReservations(fecha);

                entries.Add(new ChartEntry(citas.Count)
                {
                    Label = fecha.ToString("MMM"),
                    ValueLabel = citas.Count.ToString(),
                    Color = colores[5 - i],
                    TextColor = SKColor.Parse("#232323"),
                    ValueLabelColor = SKColor.Parse("#232323")
                });
            }

            return entries;
        }

        private void OnRankingChartTypeChanged(object sender, EventArgs e)
        {
            CargarRankingBarberos().ConfigureAwait(false);
        }

        private async Task CargarRankingBarberos()
        {
            try
            {
                var barberos = await _authService.ObtenerBarberos();
                var entries = new List<ChartEntry>();
                var colores = new[]
                {
                    SKColor.Parse("#FFD700"),
                    SKColor.Parse("#c0aa4f"),
                    SKColor.Parse("#DAA520")
                };

                int colorIndex = 0;
                foreach (var barbero in barberos.Take(3))
                {
                    var citas = await _reservationService.GetReservationsById(barbero.Cedula);
                    entries.Add(new ChartEntry(citas.Count)
                    {
                        Label = barbero.Nombre,
                        ValueLabel = citas.Count.ToString(),
                        Color = colores[colorIndex++],
                        TextColor = SKColor.Parse("#232323"),
                        ValueLabelColor = SKColor.Parse("#232323")
                    });
                }

                Chart chart;
                if (RankingChartTypePicker.SelectedIndex == 0)
                {
                    chart = new BarChart
                    {
                        Entries = entries.OrderByDescending(e => e.Value).ToList(),
                        LabelTextSize = 40,
                        Margin = 50,
                        LabelOrientation = Orientation.Horizontal,
                        ValueLabelOrientation = Orientation.Horizontal,
                        BackgroundColor = SKColor.Parse("#fffbe6")
                    };
                }
                else
                {
                    chart = new LineChart
                    {
                        Entries = entries.OrderByDescending(e => e.Value).ToList(),
                        LabelTextSize = 40,
                        Margin = 50,
                        LabelOrientation = Orientation.Horizontal,
                        ValueLabelOrientation = Orientation.Horizontal,
                        BackgroundColor = SKColor.Parse("#fffbe6")
                    };
                }

                RankingBarberosChart.Chart = chart;
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error al cargar ranking: {ex.Message}", Colors.Red, Colors.White);
            }
        }

        private async Task CargarClientesFrecuentes()
        {
            try
            {
                var fechaActual = DateTime.Now; // Agregar fecha actual como parámetro
                var todasLasCitas = await _reservationService.GetReservations(fechaActual); // Corregir llamada al método

                var clientesFrecuentes = todasLasCitas
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

                // Log temporal para depuración
                await AppUtils.MostrarSnackbar($"Clientes frecuentes encontrados: {clientesDetallados.Count}", Colors.Green, Colors.White);

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
        public string Position { get; set; }
    }

    public class ClienteFrecuente
    {
        public string Nombre { get; set; }
        public int Visitas { get; set; }
    }
}