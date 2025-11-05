using System.Collections.ObjectModel;
using System.Text.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class HistorialSolicitudesPage : ContentPage, INotifyPropertyChanged
    {
        public ObservableCollection<SolicitudAdministradorExtendida> Solicitudes { get; set; } = new();
        private readonly HttpClient _httpClient;
        public HistorialSolicitudesPage()
        {
            InitializeComponent();
            _httpClient = App.Current!.Handler.MauiContext!.Services.GetService<AuthService>()!._BaseClient;
            BindingContext = this;
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarHistorial();
        }
        private async Task CargarHistorial()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/solicitudes/historial");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var solicitudes = JsonSerializer.Deserialize<List<SolicitudAdministradorExtendida>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    Solicitudes.Clear();
                    if (solicitudes != null && solicitudes.Count > 0)
                    {
                        foreach (var s in solicitudes)
                            Solicitudes.Add(s);
                        SolicitudesCollection.IsVisible = true;
                        EmptyStateLayout.IsVisible = false;
                    }
                    else
                    {
                        SolicitudesCollection.IsVisible = false;
                        EmptyStateLayout.IsVisible = true;
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    // Extiende el modelo para exponer el icono según el estado
    public class SolicitudAdministradorExtendida : SolicitudAdministrador
    {
        public string IconoEstado => Estado?.ToLower() switch
        {
            "aprobado" => "✓",
            "rechazado" => "✗",
            _ => "🟡"
        };
    }
}
