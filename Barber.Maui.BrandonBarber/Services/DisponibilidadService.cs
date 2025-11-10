using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;
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

        public async Task<DisponibilidadModel?> GetDisponibilidadPorBarbero(long cedula)
        {
            try
            {
                string url = $"api/disponibilidad/by-barberId/{cedula}";
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
        // Guardar plantilla semanal en SecureStorage
        public async Task<bool> GuardarPlantillaSemanal(PlantillaDisponibilidadModel plantilla)
        {
            try
            {
                var json = JsonSerializer.Serialize(plantilla);
                Console.WriteLine($"🔹 JSON a guardar: {json}");

                await SecureStorage.Default.SetAsync($"plantilla_semanal_{plantilla.BarberoId}", json);

                Console.WriteLine($"✅ Plantilla guardada para barbero: {plantilla.BarberoId}");

                // Verificar que se guardó
                var verificacion = await SecureStorage.Default.GetAsync($"plantilla_semanal_{plantilla.BarberoId}");
                Console.WriteLine($"🔹 Verificación: {verificacion}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al guardar plantilla: {ex.Message}");
                Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
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

        // Aplicar plantilla a un rango de fechas
        public async Task<bool> AplicarPlantillaARango(long barberoId, DateTime fechaInicio, DateTime fechaFin)
        {
            try
            {
                var plantilla = await ObtenerPlantillaSemanal(barberoId);
                if (plantilla == null)
                    return false;

                var fechaActual = fechaInicio.Date;
                while (fechaActual <= fechaFin.Date)
                {
                    // Obtener día de la semana en español
                    var diaSemana = ObtenerDiaSemanaEspanol(fechaActual.DayOfWeek);

                    // Si hay horarios configurados para este día en la plantilla
                    if (plantilla.HorariosPorDia.ContainsKey(diaSemana))
                    {
                        var disponibilidad = new DisponibilidadModel
                        {
                            Id = 0,
                            Fecha = fechaActual,
                            BarberoId = barberoId,
                            HorariosDict = plantilla.HorariosPorDia[diaSemana]
                        };

                        await GuardarDisponibilidad(disponibilidad);
                    }

                    fechaActual = fechaActual.AddDays(1);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al aplicar plantilla: {ex.Message}");
                return false;
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
        // Agregar este método en DisponibilidadService.cs

        public async Task<PlantillaDisponibilidadModel?> ReconstruirPlantillaDesdeDisponibilidades(long barberoId)
        {
            try
            {
                // Obtener disponibilidades del mes actual
                var year = DateTime.Today.Year;
                var month = DateTime.Today.Month;

                var disponibilidades = await GetDisponibilidadPorMes(barberoId, year, month);

                if (disponibilidades == null || !disponibilidades.Any())
                {
                    Console.WriteLine("⚠️ No hay disponibilidades guardadas en la BD");
                    return null;
                }

                Console.WriteLine($"✅ Se encontraron {disponibilidades.Count} disponibilidades en la BD");

                // Crear la plantilla vacía
                var plantilla = new PlantillaDisponibilidadModel
                {
                    BarberoId = barberoId,
                    HorariosPorDia = new Dictionary<string, Dictionary<string, bool>>()
                };

                // Inicializar cada día con horarios vacíos
                string[] diasSemana = ["Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo"];
                foreach (var dia in diasSemana)
                {
                    plantilla.HorariosPorDia[dia] = new Dictionary<string, bool>();
                }

                // Agrupar disponibilidades por día de la semana
                var disponibilidadesPorDia = disponibilidades
                    .GroupBy(d => ObtenerDiaSemanaEspanol(d.Fecha.DayOfWeek))
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Para cada día de la semana que tenga disponibilidades
                foreach (var grupo in disponibilidadesPorDia)
                {
                    var diaSemana = grupo.Key;
                    var disponibilidadesDia = grupo.Value;

                    Console.WriteLine($"📅 Procesando {diaSemana}: {disponibilidadesDia.Count} registros");

                    // Crear un diccionario temporal para contar ocurrencias de cada horario
                    var conteoHorarios = new Dictionary<string, int>();

                    // Contar cuántas veces cada horario está disponible
                    foreach (var disp in disponibilidadesDia)
                    {
                        if (disp.HorariosDict != null)
                        {
                            foreach (var horario in disp.HorariosDict)
                            {
                                if (!conteoHorarios.ContainsKey(horario.Key))
                                    conteoHorarios[horario.Key] = 0;

                                if (horario.Value) // Si está disponible
                                    conteoHorarios[horario.Key]++;
                            }
                        }
                    }

                    // Determinar el patrón más común (si más del 50% de las veces está disponible)
                    var totalRegistros = disponibilidadesDia.Count;
                    foreach (var horario in conteoHorarios)
                    {
                        var porcentajeDisponible = (double)horario.Value / totalRegistros;
                        plantilla.HorariosPorDia[diaSemana][horario.Key] = porcentajeDisponible > 0.5;

                        Console.WriteLine($"  - {horario.Key}: {horario.Value}/{totalRegistros} veces disponible = {porcentajeDisponible:P0}");
                    }
                }

                Console.WriteLine("✅ Plantilla reconstruida exitosamente desde la BD");
                return plantilla;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al reconstruir plantilla: {ex.Message}");
                Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                return null;
            }
        }
    }
}