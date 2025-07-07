public class ServicioService
{
    private readonly HttpClient _httpClient;

    public ServicioService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ServicioModel>> GetServiciosAsync()
    {
        var response = await _httpClient.GetAsync("api/servicios");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<List<ServicioModel>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task CrearServicioAsync(ServicioModel servicio)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(servicio);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("api/servicios", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task EditarServicioAsync(ServicioModel servicio)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(servicio);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PutAsync($"api/servicios/{servicio.Id}", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task EliminarServicioAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/servicios/{id}");
        response.EnsureSuccessStatusCode();
    }
}