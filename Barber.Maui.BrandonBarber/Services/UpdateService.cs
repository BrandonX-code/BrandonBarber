using System.Text.Json;

namespace Barber.Maui.BrandonBarber.Services
{
    public class UpdateService
    {
        private readonly HttpClient _httpClient;
        private const string CURRENT_VERSION = "1.0"; // ⚠️ CAMBIAR SEGÚN TU VERSIÓN ACTUAL

        public UpdateService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                Console.WriteLine("🔍 Verificando actualizaciones...");

                var response = await _httpClient.GetAsync("api/update/check");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error al verificar actualización: {response.StatusCode}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"📦 Respuesta del servidor: {json}");

                var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (updateInfo == null)
                {
                    Console.WriteLine("⚠️ No se pudo deserializar la información de actualización");
                    return null;
                }

                // Comparar versiones
                var currentVersion = Version.Parse(CURRENT_VERSION);
                var serverVersion = Version.Parse(updateInfo.Version);

                Console.WriteLine($"📱 Versión actual: {currentVersion}");
                Console.WriteLine($"☁️ Versión del servidor: {serverVersion}");

                if (serverVersion > currentVersion)
                {
                    Console.WriteLine("✅ Nueva versión disponible!");
                    return updateInfo;
                }

                Console.WriteLine("✅ App actualizada");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al verificar actualizaciones: {ex.Message}");
                return null;
            }
        }
    }

    public class UpdateInfo
    {
        public string Version { get; set; } = string.Empty;
        public string ApkUrl { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }
}