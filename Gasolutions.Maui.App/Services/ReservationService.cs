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

        public ReservationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            URL = _httpClient.BaseAddress.ToString();
        }
        public async Task<bool> AddReservation(CitaModel cita)
        {
            try
            {
                var json = JsonSerializer.Serialize(cita);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"🔹 Enviando solicitud a {_httpClient.BaseAddress}citas");
                Console.WriteLine($"🔹 Datos enviados: {json}");

                var response = await _httpClient.PostAsync("citas", content);

                string responseMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔹 Código de estado API: {response.StatusCode}");
                Console.WriteLine($"🔹 Respuesta API: {responseMessage}");

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Ya existe una cita en esta fecha y hora. Elija otro horario.", "Aceptar");
                    return false;
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

        public async Task<List<CitaModel>> GetReservations(DateTime fecha)
        {
            string url = $"{_httpClient.BaseAddress}/by-date/{fecha:yyyy-MM-dd}";

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

        public async Task<List<CitaModel>> GetReservationsById(long cedula)
        {
            try
            {
                var response = await _httpClient.GetAsync(_httpClient.BaseAddress + $"/{cedula}");

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
                var response = await _httpClient.DeleteAsync($"{_httpClient.BaseAddress}/{id}");

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

        public static bool ExistsReservation(int cedula)
        {
            return _reservations.Any(c => c.Cedula == cedula);
        }
    }
}