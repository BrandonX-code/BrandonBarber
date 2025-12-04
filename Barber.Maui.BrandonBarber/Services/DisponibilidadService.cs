using Microsoft.Maui.Storage;
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
                        // No hay disponibilidad registrada para esta fecha y barbero
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
            var fechaActual = DateTime.Now.Date.ToString("yyyy-MM-dd"); // o DateTime.Today
            var response = await _httpClient.GetAsync($"api/disponibilidad/barbero/{barberoId}/fecha/{fechaActual}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<DisponibilidadModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<DisponibilidadModel>();
            }

            return new List<DisponibilidadModel>();
        }


        public async Task<bool> GuardarDisponibilidad(DisponibilidadModel disponibilidad)
        {
            try
            {
                var disponibilidadParaEnviar = new
                {
                    id = disponibilidad.Id,
                    fecha = disponibilidad.Fecha.ToString("yyyy-MM-ddTHH:mm:ss"), // formatea fecha correcta
                    barberoId = disponibilidad.BarberoId,
                    horarios = JsonSerializer.Serialize(disponibilidad.HorariosDict), // serializa como string
                    horariosDict = disponibilidad.HorariosDict  // envia como objeto
                };

                var json = JsonSerializer.Serialize(disponibilidadParaEnviar);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/disponibilidad", content);

                string responseMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔹 Código de estado API: {response.StatusCode}");
                Console.WriteLine($"🔹 Respuesta API: {responseMessage}");

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

        // Obtener plantilla semanal
        public async Task<PlantillaDisponibilidadModel?> ObtenerPlantillaSemanal(long barberoId)
        {
            try
            {
                var json = await SecureStorage.Default.GetAsync($"plantilla_semanal_{barberoId}");
                if (string.IsNullOrEmpty(json))
                    return null;

                return JsonSerializer.Deserialize<PlantillaDisponibilidadModel>(json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al obtener plantilla: {ex.Message}");
                return null;
            }
        }

        // Método auxiliar para obtener día de la semana en español
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
        // Obtener todas las disponibilidades de un barbero para un mes específico
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
        private TimeSpan ParseHoraSeguro(string hora)
        {
            // Normalizamos todos los formatos comunes
            hora = hora
                .Replace("a.m.", "AM", StringComparison.OrdinalIgnoreCase)
                .Replace("p.m.", "PM", StringComparison.OrdinalIgnoreCase)
                .Replace("a. m.", "AM", StringComparison.OrdinalIgnoreCase)
                .Replace("p. m.", "PM", StringComparison.OrdinalIgnoreCase)
                .Replace("am", "AM", StringComparison.OrdinalIgnoreCase)
                .Replace("pm", "PM", StringComparison.OrdinalIgnoreCase)
                .Trim();

            // Intentar con formato estándar
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

        public async Task<DisponibilidadSemanalModel?> ObtenerDisponibilidadSemanal(long barberoId)
        {
            try
            {
                var json = await SecureStorage.Default.GetAsync($"disponibilidad_semanal_{barberoId}");
                if (string.IsNullOrEmpty(json))
                {
                    // Retornar configuración por defecto
                    return new DisponibilidadSemanalModel
                    {
                        BarberoId = barberoId,
                        Dias = new List<DiaDisponibilidadModel>
                        {
                            new() { NombreDia = "Lunes", Habilitado = true, HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(18, 0, 0) },
                            new() { NombreDia = "Martes", Habilitado = true, HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(18, 0, 0) },
                            new() { NombreDia = "Miércoles", Habilitado = true, HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(18, 0, 0) },
                            new() { NombreDia = "Jueves", Habilitado = true, HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(18, 0, 0) },
                            new() { NombreDia = "Viernes", Habilitado = true, HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(18, 0, 0) },
                            new() { NombreDia = "Sábado", Habilitado = true, HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(18, 0, 0) },
                            new() { NombreDia = "Domingo", Habilitado = false, HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(18, 0, 0) }
                        }
                    };
                }

                return JsonSerializer.Deserialize<DisponibilidadSemanalModel>(json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al obtener disponibilidad semanal: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> GuardarDisponibilidadSemanal(DisponibilidadSemanalModel disponibilidad)
        {
            try
            {
                var json = JsonSerializer.Serialize(disponibilidad);
                await SecureStorage.Default.SetAsync($"disponibilidad_semanal_{disponibilidad.BarberoId}", json);

                // Aplicar al mes actual automáticamente
                await AplicarDisponibilidadSemanalAMes(disponibilidad.BarberoId, DateTime.Today);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al guardar disponibilidad semanal: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AplicarDisponibilidadSemanalAMes(long barberoId, DateTime fecha)
        {
            try
            {
                var disponibilidadSemanal = await ObtenerDisponibilidadSemanal(barberoId);
                if (disponibilidadSemanal == null) return false;

                var primerDia = new DateTime(fecha.Year, fecha.Month, 1);
                var ultimoDia = primerDia.AddMonths(1).AddDays(-1);

                var fechaActual = primerDia;
                while (fechaActual <= ultimoDia)
                {
                    var diaSemana = ObtenerDiaSemanaEspanol(fechaActual.DayOfWeek);
                    var diaConfig = disponibilidadSemanal.Dias.FirstOrDefault(d => d.NombreDia == diaSemana);

                    if (diaConfig != null && diaConfig.Habilitado)
                    {
                        // Generar franjas cada 40 minutos
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
    }
}