using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
namespace Barber.Maui.BrandonBarber.Services
{
    public class DisponibilidadService
    {
        private readonly HttpClient _httpClient;
        private readonly string URL;
        public static DisponibilidadModel? CurrentUser { get; set; }

        public DisponibilidadService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            URL = _httpClient.BaseAddress!.ToString();
        }

        public async Task<DisponibilidadModel?> GetDisponibilidad(DateTime fecha, long barberoId)
        {
            try
            {
                string url = $"api/disponibilidad/by-date?fecha={fecha:yyyy-MM-dd}&barberoId={barberoId}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return null;
                    }
                    Debug.WriteLine($"❌ Error al obtener disponibilidad: {response.StatusCode}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var disponibilidad = JsonSerializer.Deserialize<DisponibilidadModel>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return disponibilidad;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Excepción al obtener disponibilidad: {ex.Message}");
                return null;
            }
        }

        public async Task<List<DisponibilidadModel>> GetDisponibilidadActualPorBarbero(long barberoId)
        {
            var fechaActual = DateTime.Now.Date.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetAsync($"api/disponibilidad/barbero/{barberoId}/fecha/{fechaActual}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<DisponibilidadModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<DisponibilidadModel>();
            }

            return new List<DisponibilidadModel>();
        }

        public async Task<List<DisponibilidadModel>> GetDisponibilidadPorMes(long barberoId, int year, int month)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/disponibilidad/barbero/{barberoId}/mes/{year}/{month}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<DisponibilidadModel>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new List<DisponibilidadModel>();
                }
                return new List<DisponibilidadModel>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al obtener disponibilidad del mes: {ex.Message}");
                return new List<DisponibilidadModel>();
            }
        }

        public async Task<bool> GuardarDisponibilidad(DisponibilidadModel disponibilidad)
        {
            try
            {
                var disponibilidadParaEnviar = new
                {
                    id = disponibilidad.Id,
                    fecha = disponibilidad.Fecha.ToString("yyyy-MM-ddTHH:mm:ss"),
                    barberoId = disponibilidad.BarberoId,
                    horarios = JsonSerializer.Serialize(disponibilidad.HorariosDict),
                    horariosDict = disponibilidad.HorariosDict
                };

                var json = JsonSerializer.Serialize(disponibilidadParaEnviar);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/disponibilidad", content);

                if (!response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Error en API: {responseContent}");
                    await Application.Current.MainPage.DisplayAlert("Error", $"No se pudo guardar la disponibilidad: {responseContent}", "Aceptar");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al conectar con la API: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", "Error de conexión con el servidor.", "Aceptar");
                return false;
            }
        }

        // Métodos públicos para gestión desde la página
        public Task<DisponibilidadSemanalModel?> ObtenerDisponibilidadSemanalDesdeApi(long barberoId)
        => ObtenerDisponibilidadSemanalDesdeApiImpl(barberoId);

        public Task<bool> AplicarDisponibilidadSemanalAMesApi(long barberoId, DateTime fecha, DisponibilidadSemanalModel semanal)
        => AplicarDisponibilidadSemanalAMesApiImpl(barberoId, fecha, semanal);

        public Task<bool> AplicarDisponibilidadSemanalASemanaActualApi(long barberoId, DateTime fecha, DisponibilidadSemanalModel semanal)
        => AplicarDisponibilidadSemanalASemanaActualApiImpl(barberoId, fecha, semanal);

        // Implementaciones internas
        private async Task<DisponibilidadSemanalModel?> ObtenerDisponibilidadSemanalDesdeApiImpl(long barberoId)
        {
            // Traer la disponibilidad del mes actual y reconstruir la semanal
            var hoy = DateTime.Today;
            var primerDia = new DateTime(hoy.Year, hoy.Month, 1);
            var mes = await GetDisponibilidadPorMes(barberoId, hoy.Year, hoy.Month);
            var diasSemana = new List<DiaDisponibilidadModel>();
            var diasNombres = new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" };
            foreach (var nombre in diasNombres)
            {
                var dia = mes.FirstOrDefault(d => ObtenerDiaSemanaEspanol(d.Fecha.DayOfWeek) == nombre);
                if (dia != null && dia.HorariosDict.Any(h => h.Value))
                {
                    var primerHorario = dia.HorariosDict.Keys.First();
                    var ultimaHorario = dia.HorariosDict.Keys.Last();
                    var horaInicio = ParseHoraSeguro(primerHorario.Split('-')[0].Trim());
                    var horaFin = ParseHoraSeguro(ultimaHorario.Split('-')[1].Trim());
                    diasSemana.Add(new DiaDisponibilidadModel
                    {
                        NombreDia = nombre,
                        Habilitado = true,
                        HoraInicio = horaInicio,
                        HoraFin = horaFin
                    });
                }
                else
                {
                    diasSemana.Add(new DiaDisponibilidadModel
                    {
                        NombreDia = nombre,
                        Habilitado = false,
                        HoraInicio = new TimeSpan(9, 0, 0),
                        HoraFin = new TimeSpan(18, 0, 0)
                    });
                }
            }
            return new DisponibilidadSemanalModel { BarberoId = barberoId, Dias = diasSemana };
        }

        private async Task<bool> AplicarDisponibilidadSemanalAMesApiImpl(long barberoId, DateTime fecha, DisponibilidadSemanalModel semanal)
        {
            try
            {
                // Eliminar todas las disponibilidades del mes antes de crear nuevas
                await EliminarDisponibilidadMes(barberoId, fecha.Year, fecha.Month);
                var primerDia = new DateTime(fecha.Year, fecha.Month, 1);
                var ultimoDia = primerDia.AddMonths(1).AddDays(-1);
                var fechaActual = primerDia;
                while (fechaActual <= ultimoDia)
                {
                    var nombreDia = ObtenerDiaSemanaEspanol(fechaActual.DayOfWeek);
                    var diaConfig = semanal.Dias.FirstOrDefault(d => d.NombreDia == nombreDia);
                    if (diaConfig != null && diaConfig.Habilitado)
                    {
                        var horarios = GenerarHorariosDesdeRango(diaConfig.HoraInicio, diaConfig.HoraFin);
                        var disponibilidad = new DisponibilidadModel
                        {
                            Id = 0,
                            Fecha = fechaActual,
                            BarberoId = barberoId,
                            HorariosDict = horarios
                        };
                        await GuardarDisponibilidad(disponibilidad);
                    }
                    fechaActual = fechaActual.AddDays(1);
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al aplicar disponibilidad al mes: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> AplicarDisponibilidadSemanalASemanaActualApiImpl(long barberoId, DateTime fecha, DisponibilidadSemanalModel semanal)
        {
            try
            {
                // Eliminar todas las disponibilidades de la semana antes de crear nuevas
                await EliminarDisponibilidadSemana(barberoId, fecha);
                int dayOfWeek = (int)fecha.DayOfWeek;
                int daysToMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
                var primerDiaSemana = fecha.Date.AddDays(-daysToMonday); // Lunes
                for (int i = 0; i < 7; i++)
                {
                    var diaActual = primerDiaSemana.AddDays(i);
                    var nombreDia = ObtenerDiaSemanaEspanol(diaActual.DayOfWeek);
                    var diaConfig = semanal.Dias.FirstOrDefault(d => d.NombreDia == nombreDia);
                    if (diaConfig != null && diaConfig.Habilitado)
                    {
                        var horarios = GenerarHorariosDesdeRango(diaConfig.HoraInicio, diaConfig.HoraFin);
                        var disponibilidad = new DisponibilidadModel
                        {
                            Id = 0,
                            Fecha = diaActual,
                            BarberoId = barberoId,
                            HorariosDict = horarios
                        };
                        await GuardarDisponibilidad(disponibilidad);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al aplicar disponibilidad a la semana: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EliminarDisponibilidadMes(long barberoId, int year, int month)
        {
            var response = await _httpClient.DeleteAsync($"api/disponibilidad/barbero/{barberoId}/mes/{year}/{month}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> EliminarDisponibilidadSemana(long barberoId, DateTime fecha)
        {
            // Calcular el lunes de la semana de la fecha dada
            int dayOfWeek = (int)fecha.DayOfWeek;
            int daysToMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
            var lunes = fecha.Date.AddDays(-daysToMonday);
            var fechaIso = lunes.ToString("yyyy-MM-dd");
            var response = await _httpClient.DeleteAsync($"api/disponibilidad/barbero/{barberoId}/semana/{fechaIso}");
            return response.IsSuccessStatusCode;
        }

        // ...resto de métodos existentes (sin SecureStorage)...

        private string ObtenerDiaSemanaEspanol(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "Lunes",
                DayOfWeek.Tuesday => "Martes",
                DayOfWeek.Wednesday => "Miércoles",
                DayOfWeek.Thursday => "Jueves",
                DayOfWeek.Friday => "Viernes",
                DayOfWeek.Saturday => "Sábado",
                DayOfWeek.Sunday => "Domingo",
                _ => ""
            };
        }

        private TimeSpan ParseHoraSeguro(string hora)
        {
            hora = hora
            .Replace("a.m.", "AM", StringComparison.OrdinalIgnoreCase)
            .Replace("p.m.", "PM", StringComparison.OrdinalIgnoreCase)
            .Replace("a. m.", "AM", StringComparison.OrdinalIgnoreCase)
            .Replace("p. m.", "PM", StringComparison.OrdinalIgnoreCase)
            .Replace("am", "AM", StringComparison.OrdinalIgnoreCase)
            .Replace("pm", "PM", StringComparison.OrdinalIgnoreCase)
            .Trim();
            if (DateTime.TryParseExact(hora,
            new[] { "hh:mm tt", "h:mm tt" },
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var dt))
            {
                return dt.TimeOfDay;
            }
            throw new FormatException($"Formato de hora inválido: '{hora}'");
        }

        private Dictionary<string, bool> GenerarHorariosDesdeRango(TimeSpan inicio, TimeSpan fin)
        {
            var horarios = new Dictionary<string, bool>();
            var horaActual = inicio;
            var duracion = TimeSpan.FromMinutes(40);
            while (horaActual + duracion <= fin)
            {
                var siguienteHora = horaActual + duracion;
                var formato = $"{FormatearHora(horaActual)} - {FormatearHora(siguienteHora)}";
                horarios[formato] = true;
                horaActual = siguienteHora;
            }
            return horarios;
        }

        private string FormatearHora(TimeSpan hora)
        {
            var dt = DateTime.Today.Add(hora);
            return dt.ToString("hh:mm tt");
        }

        public List<FranjaHorariaModel> GenerarFranjasHorarias(Dictionary<string, bool> horariosDisponibles)
        {
            var franjas = new List<FranjaHorariaModel>();
            var duracionFranja = TimeSpan.FromMinutes(40);
            foreach (var horario in horariosDisponibles.Where(h => h.Value))
            {
                var partes = horario.Key.Split('-');
                var horaInicio = ParseHoraSeguro(partes[0].Trim());
                var horaFin = ParseHoraSeguro(partes[1].Trim());
                var horaActual = horaInicio;
                while (horaActual + duracionFranja <= horaFin)
                {
                    franjas.Add(new FranjaHorariaModel
                    {
                        HoraInicio = horaActual,
                        HoraFin = horaActual + duracionFranja,
                        EstaDisponible = true
                    });
                    horaActual += duracionFranja;
                }
            }
            return franjas.OrderBy(f => f.HoraInicio).ToList();
        }
    }
}