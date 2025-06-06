using Gasolutions.Maui.App.Models;
using Gasolutions.Maui.App.Services;
using System.Collections.ObjectModel;

namespace Gasolutions.Maui.App.Pages
{
    public partial class ListarBarberosPage : ContentPage
    {
        public readonly AuthService _authService;
        private ObservableCollection<UsuarioModels> _todosLosBarberos;
        private ObservableCollection<UsuarioModels> _barberosFiltrados;

        public ObservableCollection<UsuarioModels> BarberosFiltrados
        {
            get => _barberosFiltrados;
            set
            {
                _barberosFiltrados = value;
                OnPropertyChanged();
            }
        }

        public ListarBarberosPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            _todosLosBarberos = new ObservableCollection<UsuarioModels>();
            _barberosFiltrados = new ObservableCollection<UsuarioModels>();

            BindingContext = this;
            LoadBarberos();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadBarberos();
        }

        private async Task LoadBarberos()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;
                ContentContainer.IsVisible = false;

                var response = await _authService._BaseClient.GetAsync("api/auth");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var usuarios = System.Text.Json.JsonSerializer.Deserialize<List<UsuarioModels>>(jsonContent,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var barberos = usuarios?.Where(u => u.Rol.ToLower() == "barbero").ToList() ?? new List<UsuarioModels>();

                    _todosLosBarberos.Clear();
                    _barberosFiltrados.Clear();

                    foreach (var barbero in barberos)
                    {
                        _todosLosBarberos.Add(barbero);
                        _barberosFiltrados.Add(barbero);
                    }

                    UpdateStats();
                }
                else
                {
                    await DisplayAlert("Error", "No se pudieron cargar los barberos", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al cargar los barberos: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
                ContentContainer.IsVisible = true;
                EmptyStateFrame.IsVisible = !_barberosFiltrados.Any();
            }
        }

        private void UpdateStats()
        {
            TotalBarberosLabel.Text = _todosLosBarberos.Count.ToString();
            BarberosActivosLabel.Text = _todosLosBarberos.Count.ToString(); // o lógica adicional si hay campo activo
        }



        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = e.NewTextValue?.ToLower() ?? string.Empty;

            _barberosFiltrados.Clear();

            var filtered = string.IsNullOrWhiteSpace(searchText)
                ? _todosLosBarberos
                : _todosLosBarberos.Where(b =>
                    b.Nombre.ToLower().Contains(searchText) ||
                    b.Email.ToLower().Contains(searchText) ||
                    b.Cedula.ToString().Contains(searchText));

            foreach (var barbero in filtered)
            {
                _barberosFiltrados.Add(barbero);
            }

            EmptyStateFrame.IsVisible = !_barberosFiltrados.Any();
        }

        private async void OnVerDetallesClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is UsuarioModels barbero)
            {
                var detalles = $"Nombre: {barbero.Nombre}\n" +
                               $"Email: {barbero.Email}\n" +
                               $"Cédula: {barbero.Cedula}\n" +
                               $"Rol: {barbero.Rol}";

                await DisplayAlert("Detalles del Barbero", detalles, "OK");
            }
        }
        private async void OnEliminarBarberoClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is UsuarioModels barbero)
            {
                var confirm = await DisplayAlert("Confirmar",
                    $"¿Está seguro de que desea eliminar al barbero {barbero.Nombre}?",
                    "Sí", "No");

                if (confirm)
                {
                    try
                    {
                        await _authService.EliminarUsuario(barbero.Cedula);

                        _todosLosBarberos.Remove(barbero);
                        _barberosFiltrados.Remove(barbero);

                        UpdateStats();
                        EmptyStateFrame.IsVisible = !_barberosFiltrados.Any();

                        await DisplayAlert("Éxito", "Barbero eliminado correctamente", "OK");
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"Error al eliminar barbero: {ex.Message}", "OK");
                    }
                }
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await LoadBarberos();
        }
    }
}


