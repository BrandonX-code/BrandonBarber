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
            _ = CargarMetricas();
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
                var fechaActual = DateTime.Now; // Para estadísticas del día actual
                var citasDelDia = await _reservationService.GetReservations(fechaActual);

                // Actualizar estadísticas generales (solo del día actual)
                TotalCitasLabel.Text = citasDelDia.Count.ToString();
                var tasaAsistencia = citasDelDia.Count > 0
                    ? (double)citasDelDia.Count(c => c.Estado == "Completada") / citasDelDia.Count * 100
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
                        Margin = 50,
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
                        Margin = 50,
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

            for (int i = 5; i >= 0; i--)
            {
                var fecha = fechaActual.AddMonths(-i);

                // Obtener todas las citas del mes completo
                var todasLasCitasDelMes = await ObtenerCitasDelMesCompleto(fecha);

                entries.Add(new ChartEntry(todasLasCitasDelMes.Count)
                {
                    Label = fecha.ToString("MMM"),
                    ValueLabel = todasLasCitasDelMes.Count.ToString(),
                    Color = colores[5 - i],
                    TextColor = SKColor.Parse("#ffffff"),
                    ValueLabelColor = SKColor.Parse("#ffffff")
                });
            }

            return entries;
        }

        private async Task<List<CitaModel>> ObtenerCitasDelMesCompleto(DateTime fecha)
        {
            var todasLasCitas = new List<CitaModel>();
            var primerDiaDelMes = new DateTime(fecha.Year, fecha.Month, 1);
            var ultimoDiaDelMes = primerDiaDelMes.AddMonths(1).AddDays(-1);
            var todasLasCitasDelSistema = await _reservationService.GetAllReservations();
            todasLasCitas = todasLasCitasDelSistema
                .Where(c => c.Fecha.Year == fecha.Year && c.Fecha.Month == fecha.Month)
                .ToList();

            return todasLasCitas;
        }

        private async Task CargarRankingBarberos()
        {
            try
            {
                // Obtén todas las citas del sistema
                var todasLasCitas = await _reservationService.GetAllReservations();
                var ranking = todasLasCitas
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

                // Determinar cuántos barberos mostrar según la cantidad total
                int maxBarberosAMostrar = DeterminarMaximoBarberos(ranking.Count);

                // Primero agregar los barberos con datos reales (limitados)
                int colorIndex = 0;
                int barberosAgregados = 0;

                foreach (var item in ranking.Take(maxBarberosAMostrar))
                {
                    var barbero = await _authService.GetUserByCedula(item.BarberoId);
                    if (barbero != null)
                    {
                        entries.Add(new ChartEntry(item.Total)
                        {
                            Label = TruncateLabel(barbero.Nombre, 10), // Truncar nombres largos
                            ValueLabel = item.Total.ToString(),
                            Color = colores[colorIndex++ % colores.Length],
                            TextColor = SKColor.Parse("#ffffff"),
                            ValueLabelColor = SKColor.Parse("#ffffff")
                        });
                        barberosAgregados++;
                    }
                }

                // Si hay más barberos de los que se muestran, agregar una entrada "Otros"
                if (ranking.Count > maxBarberosAMostrar)
                {
                    var otrosTotal = ranking.Skip(maxBarberosAMostrar).Sum(x => x.Total);
                    entries.Add(new ChartEntry(otrosTotal)
                    {
                        Label = $"Otros ({ranking.Count - maxBarberosAMostrar})",
                        ValueLabel = otrosTotal.ToString(),
                        Color = SKColor.Parse("#666666"), // Color gris para "Otros"
                        TextColor = SKColor.Parse("#ffffff"),
                        ValueLabelColor = SKColor.Parse("#ffffff")
                    });
                }

                // Solo rellenar con ceros si hay muy pocos barberos
                int minEntries = Math.Min(6, maxBarberosAMostrar);
                while (entries.Count < minEntries && ranking.Count < 3)
                {
                    entries.Add(new ChartEntry(0)
                    {
                        Label = "", // Sin etiqueta para entradas vacías
                        ValueLabel = "0",
                        Color = colores[colorIndex++ % colores.Length],
                        TextColor = SKColor.Parse("#ffffff"),
                        ValueLabelColor = SKColor.Parse("#ffffff")
                    });
                }

                // Si no hay ningún dato real, mostrar mensaje
                if (ranking.Count == 0)
                {
                    await AppUtils.MostrarSnackbar("No hay datos de barberos para mostrar en el ranking.", Colors.Orange, Colors.White);
                }

                Chart chart;
                if (RankingChartTypePicker.SelectedIndex == 0)
                {
                    chart = new BarChart
                    {
                        Entries = entries,
                        LabelTextSize = DeterminarTamañoTexto(entries.Count), // Texto más pequeño si hay muchos elementos
                        Margin = 50,
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
                        LabelTextSize = DeterminarTamañoTexto(entries.Count),
                        Margin = 50,
                        LabelOrientation = Orientation.Horizontal,
                        ValueLabelOrientation = Orientation.Horizontal,
                        BackgroundColor = SKColor.Parse("#0E2A36")
                    };
                }

                RankingBarberosChart.Chart = chart;

                // Mostrar información adicional si hay muchos barberos
                if (ranking.Count > maxBarberosAMostrar)
                {
                    await AppUtils.MostrarSnackbar($"Mostrando top {maxBarberosAMostrar} de {ranking.Count} barberos", Colors.Blue, Colors.White);
                }
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error al cargar ranking: {ex.Message}", Colors.Red, Colors.White);
            }
        }
        private int DeterminarMaximoBarberos(int totalBarberos)
        {
            if (totalBarberos <= 5) return 6; // Mostrar hasta 6 si hay pocos
            if (totalBarberos <= 10) return 8; // Mostrar hasta 8 si hay cantidad media
            return 10; // Máximo 10 barberos para evitar abarrotar el gráfico
        }

        // Método auxiliar para determinar el tamaño del texto según la cantidad de elementos
        private float DeterminarTamañoTexto(int cantidadElementos)
        {
            if (cantidadElementos <= 5) return 20f;
            if (cantidadElementos <= 8) return 18f;
            return 16f; // Texto más pequeño para muchos elementos
        }

        // Método auxiliar para truncar nombres largos
        private string TruncateLabel(string nombre, int maxLength)
        {
            if (string.IsNullOrEmpty(nombre)) return "";
            return nombre.Length <= maxLength ? nombre : nombre.Substring(0, maxLength - 2) + "..";
        }

        private async Task CargarClientesFrecuentes()
        {
            try
            {
                // Obtener todas las citas del sistema para el ranking general de clientes
                var todasLasCitas = await _reservationService.GetAllReservations();

                var clientesFrecuentes = todasLasCitas
                    .Where(c => c.Cedula > 0) // Filtrar citas con cédula válida
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
                    await AppUtils.MostrarSnackbar("No hay datos de clientes para mostrar en el ranking.", Colors.Orange, Colors.White);
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