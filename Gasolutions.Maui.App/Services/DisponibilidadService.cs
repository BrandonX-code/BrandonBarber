using Gasolutions.Maui.App.Models;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Gasolutions.Maui.App.Services
{
    public class DisponibilidadService
    {
        private readonly HttpClient _httpClient;
        private string URL;
        public static DisponibilidadModel CurrentUser { get; set; }

        public DisponibilidadService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            URL = _httpClient.BaseAddress.ToString();
        }

        public async Task<DisponibilidadModel> GetDisponibilidad(DateTime fecha, long barberoId)
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

        public async Task<DisponibilidadModel> GetDisponibilidadPorBarbero(long cedula)
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
    }
}