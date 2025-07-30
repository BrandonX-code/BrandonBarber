using System.Text.Json;

public class ServicioService
{
    private readonly HttpClient _httpClient;

    public ServicioService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ServicioModel>> GetServiciosAsync()
    {
        var admin = AuthService.CurrentUser;
        var response = await _httpClient.GetAsync($"api/servicios/{admin.IdBarberia}");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ServicioModel>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public async Task CrearServicioAsync(ServicioModel servicio)
    {
        using var formData = new MultipartFormDataContent();

        // Agregar propiedades del servicio (deben coincidir exactamente con ServicioModel)
        if (!string.IsNullOrEmpty(servicio.Nombre))
            formData.Add(new StringContent(servicio.Nombre), nameof(ServicioModel.Nombre));

        formData.Add(new StringContent(servicio.Precio.ToString()), nameof(ServicioModel.Precio));
        formData.Add(new StringContent(servicio.Imagen.ToString()), nameof(ServicioModel.Imagen));
        formData.Add(new StringContent(servicio.Id.ToString()), nameof(ServicioModel.Id));

        // Leer el archivo de imagen
        var fileBytes = await File.ReadAllBytesAsync(servicio.Imagen);
        var fileContent = new ByteArrayContent(fileBytes);

        // Obtener información del archivo
        var fileInfo = new FileInfo(servicio.Imagen);
        string fileName = fileInfo.Name;
        string mimeType = GetMimeType(fileInfo.Extension);

        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
        formData.Add(fileContent, "imageFile", fileName);

        try
        {
            var response = await _httpClient.PostAsync("api/servicios", formData);

            // Debug: Mostrar detalles del error
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Error {response.StatusCode}: {errorContent}");
                throw new HttpRequestException($"Error {response.StatusCode}: {errorContent}");
            }

            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Excepción en CrearServicioAsync: {ex.Message}");
            throw;
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

    public async Task EditarServicioAsync(ServicioModel servicio)
    {
        using var formData = new MultipartFormDataContent();

        formData.Add(new StringContent(servicio.Nombre), nameof(ServicioModel.Nombre));
        formData.Add(new StringContent(servicio.Precio.ToString()), nameof(ServicioModel.Precio));

        bool esUrl = Uri.TryCreate(servicio.Imagen, UriKind.Absolute, out var uri) &&
                     (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        if (esUrl)
        {
            formData.Add(new StringContent(servicio.Imagen), nameof(ServicioModel.Imagen));
        }
        else
        {
            if (!File.Exists(servicio.Imagen))
                throw new IOException($"Archivo no encontrado: {servicio.Imagen}");

            var fileBytes = await File.ReadAllBytesAsync(servicio.Imagen);
            var fileContent = new ByteArrayContent(fileBytes);

            var fileInfo = new FileInfo(servicio.Imagen);
            var mimeType = GetMimeType(fileInfo.Extension);

            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);

            formData.Add(fileContent, "imagenFile", fileInfo.Name);
            formData.Add(new StringContent(fileInfo.Name), nameof(ServicioModel.Imagen));
        }

        var response = await _httpClient.PutAsync($"api/servicios/{servicio.Id}", formData);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"Error {response.StatusCode}: {errorContent}");
            throw new HttpRequestException($"Error {response.StatusCode}: {errorContent}");
        }
    }


    public async Task EliminarServicioAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/servicios/{id}");
        response.EnsureSuccessStatusCode();
    }

    // Método helper para convertir Stream a byte array (útil para MAUI)
    public static async Task<byte[]> StreamToByteArrayAsync(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    // Método helper para detectar el tipo de contenido
    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "image/jpeg" // Por defecto
        };
    }
}