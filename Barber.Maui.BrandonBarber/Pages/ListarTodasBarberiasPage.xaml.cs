using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Barber.Maui.BrandonBarber.Models;
using Barber.Maui.BrandonBarber.Services;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class ListarTodasBarberiasPage : ContentPage, INotifyPropertyChanged
    {
        private readonly BarberiaService _barberiaService;
        private readonly AuthService _authService;
        public ObservableCollection<Barberia> Barberias { get; } = [];
        public ObservableCollection<Barberia> FilteredBarberias { get; } = [];

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSearchText));
                FilterBarberias();
            }
        }
        public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);
        public Command ClearSearchCommand { get; }

        public ListarTodasBarberiasPage()
        {
            InitializeComponent();
            _barberiaService = Application.Current!.Handler.MauiContext!.Services.GetService<BarberiaService>()!;
            _authService = Application.Current!.Handler.MauiContext!.Services.GetService<AuthService>()!;
            BindingContext = this;
            ClearSearchCommand = new Command(ClearSearch);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadBarberias();
        }

        private async Task LoadBarberias()
        {
            try
            {
                var barberias = await _barberiaService.GetBarberiasAsync();
                var response = await _authService._BaseClient.GetAsync("api/auth?rol=administrador");
                List<UsuarioModels> administradores = [];
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    administradores = System.Text.Json.JsonSerializer.Deserialize<List<UsuarioModels>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
                }
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Barberias.Clear();
                    if (barberias?.Count > 0 == true)
                    {
                        foreach (var barberia in barberias)
                        {
                            if (string.IsNullOrWhiteSpace(barberia.LogoUrl))
                                barberia.LogoUrl = "picture.png";
                            var admin = administradores.FirstOrDefault(a => a.Cedula == barberia.Idadministrador);
                            barberia.NombreAdministrador = admin?.Nombre ?? "";
                            Barberias.Add(barberia);
                        }
                    }
                    TotalBarberiasLabel.Text = barberias.Count.ToString();
                    FilterBarberias();
                });
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error cargando barberías: {ex.Message}", Colors.Red, Colors.White);
            }
        }

        private void FilterBarberias()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                FilteredBarberias.Clear();
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    foreach (var barberia in Barberias)
                        FilteredBarberias.Add(barberia);
                }
                else
                {
                    var searchLower = SearchText.ToLowerInvariant();
                    var filtered = Barberias.Where(b =>
                    (b.Nombre?.ToLowerInvariant().Contains(searchLower, StringComparison.InvariantCultureIgnoreCase) ?? false) ||
                    (b.Direccion?.ToLowerInvariant().Contains(searchLower, StringComparison.InvariantCultureIgnoreCase) ?? false) ||
                    (b.Telefono?.ToLowerInvariant().Contains(searchLower, StringComparison.InvariantCultureIgnoreCase) ?? false) ||
                    (b.Email?.ToLowerInvariant().Contains(searchLower, StringComparison.InvariantCultureIgnoreCase) ?? false) ||
                    (b.NombreAdministrador?.ToLowerInvariant().Contains(searchLower, StringComparison.InvariantCultureIgnoreCase) ?? false)
                    ).ToList();
                    foreach (var barberia in filtered)
                        FilteredBarberias.Add(barberia);
                }
            });
        }

        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
