using System.Net.Http.Json;

namespace Barber.Maui.BrandonBarber.Services
{
    public class AdministradorService
    {
        private readonly HttpClient _httpClient;

        public AdministradorService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<UsuarioModels>> GetAdministradoresAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<UsuarioModels>>("api/administradores") ?? [];
        }

        public async Task SuspenderAdministradorAsync(long cedula)
        {
            var response = await _httpClient.PutAsync($"api/administradores/{cedula}/suspender", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task ActivarAdministradorAsync(long cedula)
        {
            var response = await _httpClient.PutAsync($"api/administradores/{cedula}/activar", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task EditarPermisosAsync(long cedula, string permisos)
        {
            var content = new StringContent($"\"{permisos}\"", System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"api/administradores/{cedula}/permisos", content);
            response.EnsureSuccessStatusCode();
        }
    }
}
