using Gasolutions.Maui.App.Models;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
namespace Gasolutions.Maui.App.Services
{
    public class ReservationService
    {
        private static List<CitaModel> _reservations = new();
        private readonly HttpClient _httpClient;
        private string URL;
        public static DisponibilidadModel CurrentUser { get; set; }

        public ReservationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            URL = _httpClient.BaseAddress.ToString();
        }
        public async Task<List<CitaModel>> GetAllReservations()
        {
            try
            {
                var admin = AuthService.CurrentUser;
                var response = await _httpClient.GetAsync($"{_httpClient.BaseAddress}api/citas/barberos/{admin.IdBarberia}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var citas = JsonSerializer.Deserialize<List<CitaModel>>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return citas ?? new List<CitaModel>();
                }
                return new List<CitaModel>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al obtener todas las citas: {ex.Message}");
                return new List<CitaModel>();
            }
        }
        public async Task<bool> AddReservation(CitaModel cita)
        {
            //try
            //{
                var json = JsonSerializer.Serialize(cita);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"🔹 Enviando solicitud a {_httpClient.BaseAddress}api/citas");
                Console.WriteLine($"🔹 Datos enviados: {json}");

                var response = await _httpClient.PostAsync("api/citas", content);

                string responseMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔹 Código de estado API: {response.StatusCode}");
                Console.WriteLine($"🔹 Respuesta API: {responseMessage}");

                if (response.StatusCode != HttpStatusCode.Created)
                {
                    throw new Exception(responseMessage);
                }

                return response.IsSuccessStatusCode;
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"❌ Error al conectar con la API: {ex.Message}");
            //    await Application.Current.MainPage.DisplayAlert("Error", "Error de conexión con el servidor.", "Aceptar");
            //    return false;
            //}
        }

        public async Task<List<CitaModel>> GetReservations(DateTime fecha, int idBarberia)
        {
            //var admin = AuthService.CurrentUser;
            string url = $"{_httpClient.BaseAddress}api/citas/by-date/{fecha:yyyy-MM-dd}&{idBarberia}";

            try
            {
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"❌ Error al obtener citas: {response.StatusCode}");
                    return new List<CitaModel>();
                }

                var json = await response.Content.ReadAsStringAsync();
                var citas = JsonSerializer.Deserialize<List<CitaModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return citas ?? new List<CitaModel>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Excepción al obtener citas: {ex.Message}");
                return new List<CitaModel>();
            }
        }
        public async Task<List<CitaModel>> GetReservationsByBarberoAndFecha(long barberoId, DateTime fecha)
        {
            string url = $"{_httpClient.BaseAddress}api/citas/barbero/{barberoId}/fecha/{fecha:yyyy-MM-dd}";

            try
            {
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"❌ Error al obtener citas para barbero {barberoId} en {fecha:yyyy-MM-dd}: {response.StatusCode}");
                    return new List<CitaModel>();
                }

                var json = await response.Content.ReadAsStringAsync();
                var citas = JsonSerializer.Deserialize<List<CitaModel>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return citas ?? new List<CitaModel>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Excepción al obtener citas: {ex.Message}");
                return new List<CitaModel>();
            }
        }

        public async Task<List<CitaModel>> GetReservationsById(long cedula)
        {
            try
            {
                var response = await _httpClient.GetAsync(_httpClient.BaseAddress + $"api/citas/{cedula}");

                if (!response.IsSuccessStatusCode)
                    return new List<CitaModel>();

                var json = await response.Content.ReadAsStringAsync();

                var citas = JsonSerializer.Deserialize<List<CitaModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Ordenar las citas por fecha
                var sortedCitas = citas?.OrderByDescending(c => c.Fecha).ToList() ?? new List<CitaModel>();

                return sortedCitas;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Excepción al obtener citas por ID: {ex.Message}");
                return new List<CitaModel>();
            }
        }

        public async Task<bool> DeleteReservation(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_httpClient.BaseAddress}api/citas/{id}");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ Cita con ID {id} eliminada correctamente.");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Error al eliminar la cita. Código: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción al eliminar cita: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", "No se pudo eliminar la cita.", "Aceptar");
                return false;
            }
        }

        public async Task<List<CitaModel>> GetAllReservationsHistorical()
        {
            try
            {
                var admin = AuthService.CurrentUser;
                // Obtener todas las citas filtradas por el administrador actual
                var response = await _httpClient.GetAsync($"{_httpClient.BaseAddress}api/citas/barberos/{admin.IdBarberia}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var citas = JsonSerializer.Deserialize<List<CitaModel>>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return citas ?? new List<CitaModel>();
                }
                return new List<CitaModel>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al obtener todas las citas históricas: {ex.Message}");
                return new List<CitaModel>();
            }
        }

        public async Task<List<CitaModel>> GetReservationsByBarberia(int idBarberia)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_httpClient.BaseAddress}api/citas/barberia/{idBarberia}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var citas = JsonSerializer.Deserialize<List<CitaModel>>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return citas ?? new List<CitaModel>();
                }
                return new List<CitaModel>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al obtener citas de la barbería: {ex.Message}");
                return new List<CitaModel>();
            }
        }
    }
}