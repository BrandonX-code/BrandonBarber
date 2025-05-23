using Gasolutions.Maui.App.Models;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Gasolutions.Maui.App.Services
{
    public class GaleriaService
    {

        public string BaseUrl => _httpClient.BaseAddress?.ToString() ?? "";

        private readonly HttpClient _httpClient;
        private string URL;

        public GaleriaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            URL = _httpClient.BaseAddress.ToString();
        }

        public async Task<bool> SubirImagen(string rutaImagen, string descripcion = null)
        {
            try
            {
                if (!File.Exists(rutaImagen))
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "El archivo de imagen no existe.", "Aceptar");
                    return false;
                }

                using var content = new MultipartFormDataContent();

                // Leer el archivo de imagen
                var fileBytes = await File.ReadAllBytesAsync(rutaImagen);
                var fileContent = new ByteArrayContent(fileBytes);

                // Obtener información del archivo
                var fileInfo = new FileInfo(rutaImagen);
                string fileName = fileInfo.Name;
                string mimeType = GetMimeType(fileInfo.Extension);

                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
                content.Add(fileContent, "imagen", fileName);

                // Agregar metadatos si existen
                if (!string.IsNullOrEmpty(descripcion))
                {
                    content.Add(new StringContent(descripcion), "descripcion");
                }

                Console.WriteLine($"🔹 Enviando imagen a {_httpClient.BaseAddress}api/galeria/addimg");
                Console.WriteLine($"🔹 Archivo: {fileName}, Tamaño: {fileBytes.Length} bytes");

                var response = await _httpClient.PostAsync("api/galeria/addimg", content);

                string responseMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔹 Código de estado API: {response.StatusCode}");
                Console.WriteLine($"🔹 Respuesta API: {responseMessage}");

                if (!response.IsSuccessStatusCode)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Error al subir la imagen al servidor.", "Aceptar");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al subir imagen: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", "Error de conexión con el servidor.", "Aceptar");
                return false;
            }
        }

        public async Task<List<ImagenGaleriaModel>> ObtenerImagenes()
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/galeria/img");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"❌ Error al obtener imágenes: {response.StatusCode}");
                    return new List<ImagenGaleriaModel>();
                }

                var json = await response.Content.ReadAsStringAsync();
                var imagenes = JsonSerializer.Deserialize<List<ImagenGaleriaModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return imagenes ?? new List<ImagenGaleriaModel>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Excepción al obtener imágenes: {ex.Message}");
                return new List<ImagenGaleriaModel>();
            }
        }

        public async Task<ImagenGaleriaModel> ObtenerImagenPorId(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/galeria/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"❌ Error al obtener imagen con ID {id}: {response.StatusCode}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var imagen = JsonSerializer.Deserialize<ImagenGaleriaModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return imagen;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Excepción al obtener imagen por ID: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> EliminarImagen(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/galeria/{id}");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ Imagen con ID {id} eliminada correctamente.");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Error al eliminar la imagen. Código: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción al eliminar imagen: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", "No se pudo eliminar la imagen.", "Aceptar");
                return false;
            }
        }

        public async Task<bool> ActualizarImagen(int id, string nuevaDescripcion)
        {
            try
            {
                var actualizacion = new { Descripcion = nuevaDescripcion };
                var json = JsonSerializer.Serialize(actualizacion);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"api/galeria/{id}", content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ Imagen con ID {id} actualizada correctamente.");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Error al actualizar la imagen. Código: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción al actualizar imagen: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", "No se pudo actualizar la imagen.", "Aceptar");
                return false;
            }
        }

        private string GetMimeType(string extension)
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