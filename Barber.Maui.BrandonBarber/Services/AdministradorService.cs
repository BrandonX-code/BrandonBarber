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
    }
}
