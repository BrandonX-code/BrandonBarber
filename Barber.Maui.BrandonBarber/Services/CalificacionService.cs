using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;

namespace Barber.Maui.BrandonBarber.Services
{
    public class CalificacionService
    {
        private readonly HttpClient _httpClient;

        public CalificacionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

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
            return (result?.promedio ?? 0, result?.total ?? 0);
        }

        public async Task<int> ObtenerCalificacionClienteAsync(long barberoId, long clienteId)
        {
            var response = await _httpClient.GetAsync($"api/calificaciones/barbero/{barberoId}/cliente/{clienteId}");
            if (!response.IsSuccessStatusCode)
                return 0;

            var result = await response.Content.ReadFromJsonAsync<CalificacionClienteResponse>();
            return result?.puntuacion ?? 0;
        }

        private class PromedioResponse
        {
            public double promedio { get; set; }
            public int total { get; set; }
        }

        private class CalificacionClienteResponse
        {
            public int puntuacion { get; set; }
        }
    }
}
