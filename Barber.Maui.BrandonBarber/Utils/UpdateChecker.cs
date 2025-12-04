using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace Barber.Maui.BrandonBarber.Utils
{
    public class UpdateInfo
    {
        public string Version { get; set; } = "";
        public string ApkUrl { get; set; } = "";
        public string Mensaje { get; set; } = "";
    }

    public static class UpdateChecker
    {
        // Cambia esta URL por la de tu JSON en GitHub o servidor
        private const string UpdateJsonUrl = "https://brandonbarber.onrender.com/update.json";

        public static async Task<UpdateInfo?> GetLatestUpdateInfoAsync()
        {
            try
            {
                using var client = new HttpClient();
                var json = await client.GetStringAsync(UpdateJsonUrl);
                return JsonSerializer.Deserialize<UpdateInfo>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
