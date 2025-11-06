using System.Collections.ObjectModel;
using System.Text.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class HistorialSolicitudesPage : ContentPage, INotifyPropertyChanged
    {
        private ObservableCollection<SolicitudAdministradorExtendida> _solicitudes = new();
        private ObservableCollection<SolicitudAdministradorExtendida> _solicitudesFiltradas = new();

        public ObservableCollection<SolicitudAdministradorExtendida> Solicitudes
        {
            get => _solicitudes;
            set
            {
                _solicitudes = value;
                OnPropertyChanged();
                ActualizarContador();
            }
        }

        public ObservableCollection<SolicitudAdministradorExtendida> SolicitudesFiltradas
        {
            get => _solicitudesFiltradas;
            set
            {
                _solicitudesFiltradas = value;
                OnPropertyChanged();
            }
        }

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
                    SolicitudesFiltradas.Clear();

                    if (solicitudes != null && solicitudes.Count > 0)
                    {
                        foreach (var s in solicitudes)
                        {
                            Solicitudes.Add(s);
                            SolicitudesFiltradas.Add(s);
                        }
                        SolicitudesCollection.IsVisible = true;
                        EmptyStateLayout.IsVisible = false;
                    }
                    else
                    {
                        SolicitudesCollection.IsVisible = false;
                        EmptyStateLayout.IsVisible = true;
                    }

                    ActualizarContador();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = e.NewTextValue?.Trim().ToLower() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Mostrar todas las solicitudes
                SolicitudesFiltradas.Clear();
                foreach (var solicitud in Solicitudes)
                {
                    SolicitudesFiltradas.Add(solicitud);
                }
            }
            else
            {
                // Filtrar por nombre, email o teléfono
                var filtradas = Solicitudes.Where(s =>
                    (!string.IsNullOrEmpty(s.NombreSolicitante) && s.NombreSolicitante.ToLower().Contains(searchText)) ||
                    (!string.IsNullOrEmpty(s.EmailSolicitante) && s.EmailSolicitante.ToLower().Contains(searchText)) ||
                    (!string.IsNullOrEmpty(s.TelefonoSolicitante) && s.TelefonoSolicitante.Contains(searchText))
                ).ToList();

                SolicitudesFiltradas.Clear();
                foreach (var solicitud in filtradas)
                {
                    SolicitudesFiltradas.Add(solicitud);
                }
            }

            // Mostrar empty state si no hay resultados
            if (SolicitudesFiltradas.Count == 0)
            {
                SolicitudesCollection.IsVisible = false;
                EmptyStateLayout.IsVisible = true;
            }
            else
            {
                SolicitudesCollection.IsVisible = true;
                EmptyStateLayout.IsVisible = false;
            }
        }

        private void ActualizarContador()
        {
            TotalSolicitudesLabel.Text = Solicitudes.Count.ToString();
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
        public bool EsRechazada => Estado?.ToLower() == "rechazado";
    }
}