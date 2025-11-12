using Barber.Maui.BrandonBarber.Models;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Barber.Maui.BrandonBarber.Services
{
    public class PerfilUsuarioService
    {
        private readonly HttpClient _httpClient;
        private readonly string URL;

        public PerfilUsuarioService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            URL = _httpClient.BaseAddress!.ToString();
        }

        /// <summary>
        /// Obtiene el perfil de usuario por su ID o cédula
        /// </summary>
        public async Task<UsuarioModels?> GetPerfilUsuario(long cedula)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/perfiles/{cedula}");
                Console.WriteLine($"🔹 Código de estado API: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"❌ Error al obtener el perfil: {response.StatusCode}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔹 Respuesta API: {json}");

                var perfil = JsonSerializer.Deserialize<UsuarioModels>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return perfil;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Excepción al obtener el perfil: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", "Error de conexión con el servidor.", "Aceptar");
                return null;
            }
        }

        /// <summary>
        /// Guarda o actualiza el perfil de usuario
        /// </summary>
        public async Task<bool> SavePerfilUsuario(UsuarioModels perfil)
        {
            try
            {
                var json = JsonSerializer.Serialize(perfil);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"🔹 Enviando solicitud a {_httpClient.BaseAddress}api/perfiles");
                Console.WriteLine($"🔹 Datos enviados: {json}");

                HttpResponseMessage response;

                // Si el perfil ya existe, actualizarlo
                if (perfil.Cedula != 0)
                {
                    response = await _httpClient.PutAsync($"api/perfiles/{perfil.Cedula}", content);
                }
                else
                {
                    response = await _httpClient.PostAsync("api/perfiles", content);
                }

                string responseMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔹 Código de estado API: {response.StatusCode}");
                Console.WriteLine($"🔹 Respuesta API: {responseMessage}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al conectar con la API: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", "Error de conexión con el servidor.", "Aceptar");
                return false;
            }
        }
        public async Task<bool> VerificarEmailExiste(string email, long cedulaActual)
        {
            var response = await _httpClient.GetAsync($"api/perfiles/verificar-email/{email}?cedulaActual={cedulaActual}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<bool>();
            }
            return false;
        }
        /// <summary>
        /// Actualiza la imagen de perfil
        /// </summary>
        public async Task<bool> UpdateProfileImage(long userId, string imagePath)
        {
            try
            {
                // Para subir una imagen, necesitaremos leer el archivo y enviarlo como MultipartFormDataContent
                using var content = new MultipartFormDataContent();

                // Leer el archivo de imagen
                var fileBytes = await File.ReadAllBytesAsync(imagePath);
                var fileContent = new ByteArrayContent(fileBytes);

                // Obtener información del archivo
                var fileInfo = new FileInfo(imagePath);
                string fileName = fileInfo.Name;
                string mimeType = GetMimeType(fileInfo.Extension);

                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
                content.Add(fileContent, "imagen", fileName);

                var response = await _httpClient.PostAsync($"api/perfiles/{userId}/imagen", content);

                string responseMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔹 Código de estado API: {response.StatusCode}");
                Console.WriteLine($"🔹 Respuesta API: {responseMessage}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al subir la imagen: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", "Error al subir la imagen de perfil.", "Aceptar");
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