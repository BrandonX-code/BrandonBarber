using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;

namespace Barber.Maui.BrandonBarber.Services
{
    public class CalificacionService(HttpClient httpClient)
    {
        private readonly HttpClient _httpClient = httpClient;

        public async Task<bool> EnviarCalificacionAsync(CalificacionModel calificacion)
        {
            var response = await _httpClient.PostAsJsonAsync("api/calificaciones", calificacion);
            return response.IsSuccessStatusCode;
        }

        public async Task<(double promedio, int total)> ObtenerPromedioAsync(long barberoId)
        {
            var response = await _httpClient.GetAsync($"api/calificaciones/barbero/{barberoId}");
            if (!response.IsSuccessStatusCode)
                return (0, 0);

            var result = await response.Content.ReadFromJsonAsync<PromedioResponse>();
            return (result?.Promedio ?? 0, result?.Total ?? 0);
        }

        public async Task<int> ObtenerCalificacionClienteAsync(long barberoId, long clienteId)
        {
            var response = await _httpClient.GetAsync($"api/calificaciones/barbero/{barberoId}/cliente/{clienteId}");
            if (!response.IsSuccessStatusCode)
                return 0;

            var result = await response.Content.ReadFromJsonAsync<CalificacionClienteResponse>();
            return result?.Puntuacion ?? 0;
        }
        public async Task<List<CalificacionModel>> ObtenerResenasAsync(long barberoId)
        {
            var response = await _httpClient.GetAsync($"api/calificaciones/barbero/{barberoId}/reseñas");
            if (!response.IsSuccessStatusCode)
                return new List<CalificacionModel>();

            var resenas = await response.Content.ReadFromJsonAsync<List<CalificacionModel>>();
            return resenas ?? new List<CalificacionModel>();
        }

        private class PromedioResponse
        {
            public double Promedio { get; set; }
            public int Total { get; set; }
        }

        private class CalificacionClienteResponse
        {
            public int Puntuacion { get; set; }
        }
    }
}
