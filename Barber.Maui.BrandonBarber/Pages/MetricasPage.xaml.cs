using Barber.Maui.BrandonBarber.Controls;
using Barber.Maui.BrandonBarber.Models;
using Barber.Maui.BrandonBarber.Services;
using Microcharts;
using SkiaSharp;
using System.Globalization;
using Command = Microsoft.Maui.Controls.Command;
using Entry = Microcharts.ChartEntry;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class MetricasPage : ContentPage
    {
        private readonly ReservationService _reservationService;
        private readonly AuthService _authService;
        private readonly BarberiaService? _barberiaService;
        private List<Barberia>? _barberias;
        private int _barberiaSeleccionadaId; // ID de la barbería seleccionada
        private int _barberiaPickerLastIndex = -1; // Para restaurar selección si se cancela
        private readonly CultureInfo _cultura;
        private readonly RegionInfo _region;
        private bool _barberiaButtonLocked = false;
        public Command RefreshCommand { get; }

        public MetricasPage(ReservationService reservationService, AuthService authService)
        {
            InitializeComponent();
            _reservationService = reservationService;
            _authService = authService;
            _barberiaService = Application.Current!.Handler.MauiContext!.Services.GetService<BarberiaService>();
            _cultura = CultureInfo.CurrentCulture;
            _region = new RegionInfo(_cultura.Name);
            RefreshCommand = new Command(async () => await RefreshMetricas());
            BindingContext = this;
            _ = CargarBarberias();
        }

        private async Task CargarBarberias()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                long idAdministrador = AuthService.CurrentUser!.Cedula;
                _barberias = await _barberiaService!.GetBarberiasByAdministradorAsync(idAdministrador);


                PickerSection.IsVisible = _barberias.Count > 0;
                BarberiaSelectButton.IsVisible = _barberias.Count > 1;

                // Mostrar botón cambiar solo si hay más de 1 barbería
                var cambiarButton = this.FindByName<Button>("BarberiaSelectButton");
                if (cambiarButton != null)
                {
                    cambiarButton.IsVisible = _barberias.Count > 1;
                    cambiarButton.Text = "Seleccionar";
                }

                if (_barberias.Count > 0)
                {
                    // Selecciona la primera barbería por defecto
                    _barberiaSeleccionadaId = _barberias[0].Idbarberia;
                    _barberiaPickerLastIndex = 0;
                    BarberiaSelectedLabel.Text = _barberias[0].Nombre ?? "Seleccionar Barbería";
                    BarberiaTelefonoLabel.Text = _barberias[0].Telefono ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(_barberias[0].LogoUrl))
                    {
                        BarberiaLogoImage.Source = _barberias[0].LogoUrl!.StartsWith("http")
                            ? ImageSource.FromUri(new Uri(_barberias[0].LogoUrl!))
                            : ImageSource.FromFile(_barberias[0].LogoUrl);
                    }
                    else
                    {
                        BarberiaLogoImage.Source = "picture.png";
                    }
                    if (cambiarButton != null)
                    {
                        cambiarButton.Text = "Cambiar";
                    }
                    await CargarMetricas();
                }
                else
                {
                    _barberiaSeleccionadaId = 0;
                    BarberiaSelectedLabel.Text = "Seleccionar Barbería";
                    BarberiaTelefonoLabel.Text = string.Empty;
                    BarberiaLogoImage.Source = "picture.png";
                    await CargarMetricas();
                }
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error al cargar barberías: {ex.Message}", Colors.Red, Colors.White);
                _barberiaSeleccionadaId = 0;
                BarberiaSelectedLabel.Text = "Seleccionar Barbería";
                BarberiaTelefonoLabel.Text = string.Empty;
                BarberiaLogoImage.Source = "picture.png";
                await CargarMetricas();
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }

        // Elimina la lógica del popup del SelectedIndexChanged
        private void BarberiaPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            // No hacer nada aquí
        }

        // Nuevo método para mostrar el popup solo cuando el usuario toca el Picker
        private async void OnBarberiaPickerTapped(object sender, EventArgs e)
        {
            if (_barberiaButtonLocked) return;
            _barberiaButtonLocked = true;

            try
            {
                if (_barberias == null || _barberias.Count <= 1)
                    return;

                var popup = new BarberiaSelectionPopup(_barberias);
                var seleccionada = await popup.ShowAsync();

                if (seleccionada != null)
                {
                    int idx = _barberias.FindIndex(b => b.Idbarberia == seleccionada.Idbarberia);
                    if (idx >= 0)
                    {
                        _barberiaSeleccionadaId = seleccionada.Idbarberia;
                        _barberiaPickerLastIndex = idx;

                        BarberiaSelectedLabel.Text = seleccionada.Nombre ?? "Seleccionar Barbería";
                        BarberiaTelefonoLabel.Text = seleccionada.Telefono ?? string.Empty;

                        if (!string.IsNullOrWhiteSpace(seleccionada.LogoUrl))
                        {
                            BarberiaLogoImage.Source = seleccionada.LogoUrl.StartsWith("http")
                                ? ImageSource.FromUri(new Uri(seleccionada.LogoUrl))
                                : ImageSource.FromFile(seleccionada.LogoUrl);
                        }
                        else
                        {
                            BarberiaLogoImage.Source = "picture.png";
                        }

                        var cambiarButton = this.FindByName<Button>("BarberiaSelectButton");
                        if (cambiarButton != null)
                        {
                            cambiarButton.Text = "Cambiar";
                        }

                        await CargarMetricas();
                    }
                }
                else if (_barberiaPickerLastIndex >= 0 && _barberias.Count > _barberiaPickerLastIndex)
                {
                    var barberia = _barberias[_barberiaPickerLastIndex];

                    BarberiaSelectedLabel.Text = barberia.Nombre ?? "Seleccionar Barbería";
                    BarberiaTelefonoLabel.Text = barberia.Telefono ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(barberia.LogoUrl))
                    {
                        BarberiaLogoImage.Source = barberia.LogoUrl.StartsWith("http")
                            ? ImageSource.FromUri(new Uri(barberia.LogoUrl))
                            : ImageSource.FromFile(barberia.LogoUrl);
                    }
                    else
                    {
                        BarberiaLogoImage.Source = "picture.png";
                    }
                }
            }
            finally
            {
                _barberiaButtonLocked = false;
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
        private string FormatearMoneda(decimal valor)
        {
            // Símbolo de moneda según región
            string simbolo = _region.CurrencySymbol;

            if (valor >= 1_000_000)
                return $"{simbolo}{valor / 1_000_000:F1}M";
            if (valor >= 1_000)
                return $"{simbolo}{valor / 1_000:F1}K";

            // Formato con separadores de miles según la cultura
            return valor.ToString("C0", _cultura); // C0 = Currency con 0 decimales
        }
        private async Task CargarMetricas()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
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

                // ✅ CAMBIO: Solo contar las gestionadas (finalizadas o canceladas)
                var citasGestionadasMes = citasDelMes
                    .Count(c => c.Estado?.ToLower() == "finalizada" || c.Estado?.ToLower() == "cancelada");

                var citasCompletadasMes = citasDelMes
                    .Count(c => c.Estado?.ToLower() == "finalizada");

                var tasaAsistencia = citasGestionadasMes > 0
                    ? (double)citasCompletadasMes / citasGestionadasMes * 100
                    : 0;

                // Actualizar estadísticas generales
                TotalCitasLabel.Text = citasHoy.Count.ToString(); // ← CITAS DE HOY
                TasaAsistenciaLabel.Text = $"{tasaAsistencia:F1}%"; // ← TASA DEL MES

                // Los gráficos y rankings SIEMPRE muestran datos históricos (últimos 6 meses)
                await Task.WhenAll(
                    CargarGraficoAsistencia(),
                    CargarGraficoTasaAsistencia(),
                    CargarRankingBarberos(),
                    CargarClientesFrecuentes(),
                    CargarGraficoGanancias()
                );
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error al cargar métricas: {ex.Message}", Colors.Red, Colors.White);
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }

        private void OnAsistenciaChartTypeChanged(object sender, int selectedIndex)
        {
            CargarGraficoTasaAsistencia().ConfigureAwait(false);
        }
        private void OnGananciasChartTypeChanged(object sender, int selectedIndex)
        {
            CargarGraficoGanancias().ConfigureAwait(false);
        }
        private async Task CargarGraficoGanancias()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;

                var entries = await ObtenerDatosGanancias();

                Chart chart;
                if (GananciasChartControl.SelectedIndex == 0)
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

                GananciasChart.Chart = chart;
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error al cargar gráfico de ganancias: {ex.Message}", Colors.Red, Colors.White);
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }
        private async Task CargarGraficoTasaAsistencia()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                var entries = await ObtenerDatosTasaAsistencia();

                Chart chart;
                if (AsistenciaChartControl.SelectedIndex == 0)
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
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }
        private async Task<List<ChartEntry>> ObtenerDatosGanancias()
        {
            var entries = new List<ChartEntry>();
            var fechaActual = DateTime.Now;

            var colores = new[]
            {
                SKColor.Parse("#4CAF50"), // Verde
                SKColor.Parse("#66BB6A"),
                SKColor.Parse("#81C784"),
                SKColor.Parse("#A5D6A7"),
                SKColor.Parse("#C8E6C9"),
                SKColor.Parse("#E8F5E9")
            };

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

            decimal gananciasMesActual = 0;
            decimal gananciasAnioActual = 0;

            for (int i = 5; i >= 0; i--)
            {
                var fecha = fechaActual.AddMonths(-i);

                // Filtrar solo citas finalizadas del mes específico
                var citasDelMes = todasLasCitas.Where(c =>
                    c.Fecha.Year == fecha.Year &&
                    c.Fecha.Month == fecha.Month &&
                    c.Estado?.ToLower() == "finalizada").ToList();

                // Calcular ganancias del mes
                decimal gananciasMes = citasDelMes
                    .Where(c => c.ServicioPrecio.HasValue)
                    .Sum(c => c.ServicioPrecio!.Value);

                entries.Add(new ChartEntry((float)gananciasMes)
                {
                    Label = fecha.ToString("MMM"),
                    ValueLabel = FormatearMoneda(gananciasMes),
                    Color = colores[5 - i],
                    TextColor = SKColor.Parse("#ffffff"),
                    ValueLabelColor = SKColor.Parse("#ffffff")
                });

                // Acumular para resumen
                if (fecha.Month == fechaActual.Month && fecha.Year == fechaActual.Year)
                {
                    gananciasMesActual = gananciasMes;
                }

                if (fecha.Year == fechaActual.Year)
                {
                    gananciasAnioActual += gananciasMes;
                }
            }

            // Actualizar labels de resumen
            GananciasMesLabel.Text = FormatearMoneda(gananciasMesActual);
            GananciasAnioLabel.Text = FormatearMoneda(gananciasAnioActual);

            return entries;
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

                // ✅ CAMBIO IMPORTANTE: Solo contar citas gestionadas (finalizadas + canceladas)
                var citasGestionadas = citasDelMes
                    .Where(c => c.Estado?.ToLower() == "finalizada" || c.Estado?.ToLower() == "cancelada")
                    .ToList();

                // Contar solo las finalizadas
                var citasFinalizadas = citasDelMes
                    .Count(c => c.Estado?.ToLower() == "finalizada");

                // Calcular tasa: finalizadas / gestionadas * 100
                var tasaAsistencia = citasGestionadas.Count > 0
                    ? (float)((double)citasFinalizadas / citasGestionadas.Count * 100)
                    : 0f;

                // Elegir color según la tasa
                SKColor color;
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

        private void OnChartTypeChanged(object sender, int selectedIndex)
        {
            CargarGraficoAsistencia().ConfigureAwait(false);
        }

        private void OnRankingChartTypeChanged(object sender, int selectedIndex)
        {
            CargarRankingBarberos().ConfigureAwait(false);
        }

        private async Task CargarGraficoAsistencia()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                var entries = await ObtenerDatosAsistencia();

                Chart chart;
                if (ChartTypeControl.SelectedIndex == 0)
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
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }

        private async Task<List<ChartEntry>> ObtenerDatosAsistencia()
        {
            var entries = new List<ChartEntry>();
            var fechaActual = DateTime.Now;
            var colores = new[]
            {
                SKColor.Parse("#E3F7FF"), // Azul oscuro profundo
                SKColor.Parse("#A6C1E3"), // Azul medio
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
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
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
                SKColor.Parse("#E3F7FF"), // Azul oscuro profundo
                SKColor.Parse("#A6C1E3"), // Azul medio
                SKColor.Parse("#4A90C8"), // Azul cielo
                SKColor.Parse("#7FB3D5"), // Azul claro
                SKColor.Parse("#B0D4E8"), // Azul pastel
                SKColor.Parse("#D4E8F5")  // Azul muy claro
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
                if (RankingChartControl.SelectedIndex == 0)
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
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
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
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;

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

                // ✅ FILTRAR SOLO CITAS FINALIZADAS
                var fechaLimite = DateTime.Now.AddMonths(-6);
                var citasFinalizadas = todasLasCitas
            .Where(c => c.Fecha >= fechaLimite &&
                c.Estado?.ToLower() == "finalizada")
                  .ToList();

                var clientesFrecuentes = citasFinalizadas
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

                // Mostrar/ocultar según si hay datos
                ClientesFrecuentesCollection.IsVisible = clientesDetallados.Count > 0;
                ClientesFrecuentesEmptyState.IsVisible = clientesDetallados.Count == 0;

                ClientesFrecuentesCollection.ItemsSource = clientesDetallados;

                if (clientesDetallados.Count == 0)
                {
                    var mensaje = _barberiaSeleccionadaId > 0
                 ? "No hay datos de clientes para la barbería seleccionada."
                : "No hay datos de clientes para mostrar en el ranking.";
                    await AppUtils.MostrarSnackbar(mensaje, Colors.Orange, Colors.White);
                }
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error al cargar clientes frecuentes: {ex.Message}", Colors.Red, Colors.White);
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }
    }
}