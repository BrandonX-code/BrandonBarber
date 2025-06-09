using Gasolutions.Maui.App.Models;
using Gasolutions.Maui.App.Services;
using System.Collections.ObjectModel;
namespace Gasolutions.Maui.App.Pages
{
    public partial class ListarClientesPage : ContentPage
    {
        public readonly AuthService _authService;
        private ObservableCollection<UsuarioModels> _todosLosClientes;
        private ObservableCollection<UsuarioModels> _clientesFiltrados;
        public ObservableCollection<UsuarioModels> ClientesFiltrados
        {
            get => _clientesFiltrados;
            set
            {
                _clientesFiltrados = value;
                OnPropertyChanged();
            }
        }

        public ListarClientesPage(AuthService authService)
        {
            InitializeComponent();
            _authService = authService;
            _todosLosClientes = new ObservableCollection<UsuarioModels>();
            _clientesFiltrados = new ObservableCollection<UsuarioModels>();

            BindingContext = this;
            LoadClientes();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadClientes();
        }

        private async Task LoadClientes()

        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;
                ContentContainer.IsVisible = false;

                // Llamar a la API para obtener todos los usuarios
                var response = await _authService._BaseClient.GetAsync("api/auth");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var usuarios = System.Text.Json.JsonSerializer.Deserialize<List<UsuarioModels>>(jsonContent,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    // Filtrar solo los clientes
                    var clientes = usuarios?.Where(u => u.Rol.ToLower() == "cliente").ToList() ?? new List<UsuarioModels>();

                    _todosLosClientes.Clear();
                    _clientesFiltrados.Clear();

                    foreach (var cliente in clientes)
                    {
                        _todosLosClientes.Add(cliente);
                        _clientesFiltrados.Add(cliente);
                    }

                    UpdateStats();
                }
                else
                {
                    await AppUtils.MostrarSnackbar("No se pudieron cargar los clientes", Colors.Red, Colors.White);
                }
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error al cargar los clientes: {ex.Message}", Colors.Red, Colors.White);
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
                ContentContainer.IsVisible = true;

                // Mostrar estado vacío si no hay clientes
                EmptyStateFrame.IsVisible = !_clientesFiltrados.Any();
            }
        }

        private void UpdateStats()
        {
            TotalClientesLabel.Text = _todosLosClientes.Count.ToString();
            ClientesActivosLabel.Text = _todosLosClientes.Count.ToString(); // Todos los clientes se consideran activos por ahora
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = e.NewTextValue?.ToLower() ?? string.Empty;

            _clientesFiltrados.Clear();

            var filtered = string.IsNullOrWhiteSpace(searchText)
                ? _todosLosClientes
                : _todosLosClientes.Where(c =>
                    c.Nombre.ToLower().Contains(searchText) ||
                    c.Email.ToLower().Contains(searchText) ||
                    c.Cedula.ToString().Contains(searchText));

            foreach (var cliente in filtered)
            {
                _clientesFiltrados.Add(cliente);
            }

            EmptyStateFrame.IsVisible = !_clientesFiltrados.Any();
        }

        private async void OnVerDetallesClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is UsuarioModels cliente)
            {
                var detalles = $"Nombre: {cliente.Nombre}\n" +
                              $"Email: {cliente.Email}\n" +
                              $"Cédula: {cliente.Cedula}\n" +
                              $"Rol: {cliente.Rol}";

                await DisplayAlert("Detalles del Cliente", detalles, "OK");
            }
        }

        private async void OnEliminarClienteClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is UsuarioModels cliente)
            {
                var confirm = await DisplayAlert("Confirmar",
                    $"¿Está seguro de que desea eliminar al cliente {cliente.Nombre}?",
                    "Sí", "No");

                if (confirm)
                {
                    try
                    {
                        await _authService.EliminarUsuario(cliente.Cedula);

                        _todosLosClientes.Remove(cliente);
                        _clientesFiltrados.Remove(cliente);

                        UpdateStats();
                        EmptyStateFrame.IsVisible = !_clientesFiltrados.Any();

                        await AppUtils.MostrarSnackbar("Cliente eliminado correctamente", Colors.Green, Colors.White);
                    }
                    catch (Exception ex)
                    {
                        await AppUtils.MostrarSnackbar($"Error al eliminar cliente: {ex.Message}", Colors.Red, Colors.White);
                    }
                }
            }
        }


        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await LoadClientes();
        }
    }
}