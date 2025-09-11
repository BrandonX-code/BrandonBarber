using System.Text;
using System.Text.Json;

namespace Barber.Maui.BrandonBarber.Services
{
    public class BarberiaService
    {
        public string BaseUrl => _httpClient.BaseAddress?.ToString() ?? "";

        private readonly HttpClient _httpClient;
        private readonly string URL;

        public BarberiaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            URL = _httpClient.BaseAddress?.ToString() ?? "";
        }

        public async Task<List<Barberia>> GetBarberiasAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/Barberias");
                Console.WriteLine($"🔹 GET Barberias - Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"❌ Error al obtener barberías: {response.StatusCode}");
                    return new List<Barberia>();
                }

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔹 Respuesta API Barberias: {json}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var barberias = JsonSerializer.Deserialize<List<Barberia>>(json, options);
                return barberias ?? new List<Barberia>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Excepción al obtener barberías: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error",
                    "Error de conexión con el servidor al obtener barberías", "Aceptar");
                return new List<Barberia>();
            }
        }

        public async Task<List<Barberia>> GetBarberiasByAdministradorAsync(long idAdministrador)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/Barberias/administrador/{idAdministrador}");
                Console.WriteLine($"🔹 GET Barberias por Administrador {idAdministrador} - Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"❌ Error al obtener barberías del administrador: {response.StatusCode}");
                    return new List<Barberia>();
                }

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔹 Respuesta API Barberias Admin: {json}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var barberias = JsonSerializer.Deserialize<List<Barberia>>(json, options);
                return barberias ?? new List<Barberia>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Excepción al obtener barberías del administrador: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error",
                    "Error de conexión con el servidor", "Aceptar");
                return new List<Barberia>();
            }
        }

        public async Task<Barberia> GetBarberiaByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/Barberias/{id}");
                Console.WriteLine($"🔹 GET Barberia por ID {id} - Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"❌ Error al obtener barbería {id}: {response.StatusCode}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var barberia = JsonSerializer.Deserialize<Barberia>(json, options);
                return barberia;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Excepción al obtener barbería por ID: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CreateBarberiaAsync(Barberia barberia)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(barberia, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"🔹 Creando barbería: {json}");
                var response = await _httpClient.PostAsync("api/Barberias", content);

                Console.WriteLine($"🔹 POST Barberia - Status: {response.StatusCode}");
                string responseMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔹 Respuesta API: {responseMessage}");

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                await Application.Current.MainPage.DisplayAlert("Error",
                    $"Error al crear barbería: {responseMessage}", "Aceptar");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al crear barbería: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error",
                    "Error de conexión con el servidor", "Aceptar");
                return false;
            }
        }

        public async Task<bool> UpdateBarberiaAsync(Barberia barberia)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(barberia, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"🔹 Actualizando barbería ID {barberia.Idbarberia}: {json}");
                var response = await _httpClient.PutAsync($"api/Barberias/{barberia.Idbarberia}", content);

                Console.WriteLine($"🔹 PUT Barberia - Status: {response.StatusCode}");
                string responseMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔹 Respuesta API: {responseMessage}");

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                await Application.Current.MainPage.DisplayAlert("Error",
                    $"Error al actualizar barbería: {responseMessage}", "Aceptar");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al actualizar barbería: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error",
                    "Error de conexión con el servidor", "Aceptar");
                return false;
            }
        }

        public async Task<bool> DeleteBarberiaAsync(int id)
        {
            try
            {
                Console.WriteLine($"🔹 Eliminando barbería ID {id}");
                var response = await _httpClient.DeleteAsync($"api/Barberias/{id}");

                Console.WriteLine($"🔹 DELETE Barberia - Status: {response.StatusCode}");
                string responseMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔹 Respuesta API: {responseMessage}");

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                await Application.Current.MainPage.DisplayAlert("Error",
                    $"Error al eliminar barbería: {responseMessage}", "Aceptar");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al eliminar barbería: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error",
                    "Error de conexión con el servidor", "Aceptar");
                return false;
            }
        }

        public async Task<bool> UploadBarberiaLogoAsync(int barberiaId, byte[] logoBytes, string fileName)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(logoBytes);

                string mimeType = BarberiaService.GetMimeType(System.IO.Path.GetExtension(fileName));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);

                content.Add(fileContent, "file", fileName);

                Console.WriteLine($"🔹 Subiendo logo para barbería ID {barberiaId}");
                var response = await _httpClient.PostAsync($"api/Barberias/{barberiaId}/logo", content);

                Console.WriteLine($"🔹 POST Barberia Logo - Status: {response.StatusCode}");
                string responseMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔹 Respuesta API: {responseMessage}");

                if (!response.IsSuccessStatusCode)
                {
                    await Application.Current.MainPage.DisplayAlert("Error",
                        $"Error al subir logo: {responseMessage}", "Aceptar");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al subir logo: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error",
                    "Error de conexión con el servidor", "Aceptar");
                return false;
            }
        }

        private static string GetMimeType(string extension)
        {
            return extension.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }
}