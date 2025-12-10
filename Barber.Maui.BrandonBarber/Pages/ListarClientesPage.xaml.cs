using System.Text.Json;
using Barber.Maui.BrandonBarber.Controls;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class ListarClientesPage : ContentPage
    {
        public readonly AuthService _authService;
        private readonly ObservableCollection<UsuarioModels> _todosLosClientes;
        private ObservableCollection<UsuarioModels> _clientesFiltrados;
        private readonly BarberiaService? _barberiaService;
        private List<Barberia>? _barberias;
        private int? _barberiaSeleccionadaId = null;
        private bool _isNavigating = false;
        public Command RefreshCommand { get; }
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
            _barberiaService = Application.Current!.Handler.MauiContext!.Services.GetService<BarberiaService>();
            _todosLosClientes = [];
            _clientesFiltrados = [];
            RefreshCommand = new Command(async () => await RefreshClienteList());
            BindingContext = this;

            CargarBarberias();
            _ = LoadClientes();
        }

        private async void CargarBarberias()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                long idAdministrador = AuthService.CurrentUser!.Cedula;
                _barberias = await _barberiaService!.GetBarberiasByAdministradorAsync(idAdministrador);

                PickerSection.IsVisible = _barberias.Count > 0;

                // Mostrar botón cambiar solo si hay más de 1 barbería
                var cambiarButton = this.FindByName<Button>("BarberiaCambiarButton");
                if (cambiarButton != null)
                {
                    cambiarButton.IsVisible = _barberias.Count > 1;
                    cambiarButton.Text = "Seleccionar";
                }

                // Si hay solo una barbería, seleccionarla automáticamente
                if (_barberias.Count == 1)
                {
                    _barberiaSeleccionadaId = _barberias[0].Idbarberia;

                    // Actualizar la UI con la barbería seleccionada
                    var nombreLabel = this.FindByName<Label>("BarberiaNombreLabel");
                    var direccionLabel = this.FindByName<Label>("BarberiaDireccionLabel");
                    var logoImage = this.FindByName<Image>("BarberialogoImage");

                    if (nombreLabel != null)
                        nombreLabel.Text = _barberias[0].Nombre ?? "Seleccionar Barbería";

                    if (direccionLabel != null)
                        direccionLabel.Text = _barberias[0].Direccion ?? string.Empty;

                    if (logoImage != null)
                    {
                        if (!string.IsNullOrWhiteSpace(_barberias[0].LogoUrl))
                        {
                            logoImage.Source = _barberias[0].LogoUrl.StartsWith("http")
                  ? ImageSource.FromUri(new Uri(_barberias[0].LogoUrl))
             : ImageSource.FromFile(_barberias[0].LogoUrl);
                        }
                        else
                        {
                            logoImage.Source = "picture.png";
                        }
                    }

                    await LoadClientes();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudieron cargar las barberías: " + ex.Message, "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }

        private async Task RefreshClienteList()
        {
            if (ClienteRefreshView.IsRefreshing)
            {
                await LoadClientes();
                ClienteRefreshView.IsRefreshing = false;
            }
        }

        private async void OnBarberiaCambiarClicked(object sender, EventArgs e)
        {
            if (_barberias == null || _barberias.Count == 0)
                return;

            var popup = new BarberiaSelectionPopup(_barberias);
            var barberiaSeleccionada = await popup.ShowAsync();

            if (barberiaSeleccionada != null)
            {
                // Actualizar la UI usando FindByName
                var nombreLabel = this.FindByName<Label>("BarberiaNombreLabel");
                var direccionLabel = this.FindByName<Label>("BarberiaDireccionLabel");
                var logoImage = this.FindByName<Image>("BarberialogoImage");
                var cambiarButton = this.FindByName<Button>("BarberiaCambiarButton");

                if (nombreLabel != null)
                    nombreLabel.Text = barberiaSeleccionada.Nombre ?? "Seleccionar Barbería";

                if (direccionLabel != null)
                    direccionLabel.Text = barberiaSeleccionada.Direccion ?? string.Empty;

                if (logoImage != null)
                {
                    if (!string.IsNullOrWhiteSpace(barberiaSeleccionada.LogoUrl))
                    {
                        logoImage.Source = barberiaSeleccionada.LogoUrl.StartsWith("http")
                  ? ImageSource.FromUri(new Uri(barberiaSeleccionada.LogoUrl))
             : ImageSource.FromFile(barberiaSeleccionada.LogoUrl);
                    }
                    else
                    {
                        logoImage.Source = "picture.png";
                    }
                }

                // Cambiar texto del botón a "Cambiar"
                if (cambiarButton != null)
                {
                    cambiarButton.Text = "Cambiar";
                }

                // Actualizar el ID de la barbería seleccionada
                _barberiaSeleccionadaId = barberiaSeleccionada.Idbarberia;

                // Cargar los clientes de la barbería seleccionada
                await LoadClientes();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = LoadClientes();
        }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private async Task LoadClientes()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                ContentContainer.IsVisible = false;

                // Si no hay barbería seleccionada, no mostrar clientes
                if (_barberiaSeleccionadaId == null)
                {
                    _todosLosClientes.Clear();
                    _clientesFiltrados.Clear();
                    UpdateStats();
                    EmptyStateFrame.IsVisible = true;
                    return;
                }

                // Llamar a la API para obtener todos los usuarios de la barbería seleccionada
                var response = await _authService._BaseClient.GetAsync($"api/auth/cliente/{_barberiaSeleccionadaId}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var usuarios = System.Text.Json.JsonSerializer.Deserialize<List<UsuarioModels>>(jsonContent, _jsonOptions);

                    // Filtrar solo los clientes
                    var clientes = usuarios?.Where(u => string.Equals(u.Rol, "cliente", StringComparison.OrdinalIgnoreCase) && u.IdBarberia == _barberiaSeleccionadaId).ToList() ?? [];
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
                LoadingIndicator.IsLoading = false;
                ContentContainer.IsVisible = true;

                // Mostrar estado vacío si no hay clientes
                EmptyStateFrame.IsVisible = !_clientesFiltrados.Any();
            }
        }

        private void UpdateStats()
        {
            TotalClientesLabel.Text = _todosLosClientes.Count.ToString();
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = e.NewTextValue?.ToLower() ?? string.Empty;

            _clientesFiltrados.Clear();

            var filtered = string.IsNullOrWhiteSpace(searchText)
                 ? _todosLosClientes
           : _todosLosClientes.Where(c =>
                 c.Nombre!.ToLower().Contains(searchText, StringComparison.InvariantCultureIgnoreCase) ||
                  c.Email!.ToLower().Contains(searchText, StringComparison.InvariantCultureIgnoreCase) ||
          c.Cedula.ToString().Contains(searchText, StringComparison.InvariantCultureIgnoreCase));

            foreach (var cliente in filtered)
            {
                _clientesFiltrados.Add(cliente);
            }

            EmptyStateFrame.IsVisible = !_clientesFiltrados.Any();
        }

        private async void OnVerDetallesClicked(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                if (sender is Button button && button.CommandParameter is UsuarioModels barbero)
                {
                    var detallesPage = new DetalleClientePage(barbero);
                    await Navigation.PushModalAsync(detallesPage);
                }
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private async void OnEliminarClienteClicked(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                if (sender is Image image &&
          image.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap &&
           tap.CommandParameter is UsuarioModels cliente)
                {
                    var popup = new CustomAlertPopup($"¿Está seguro de que desea eliminar al cliente {cliente.Nombre}?");
                    bool confirm = await popup.ShowAsync(this);
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
            finally
            {
                _isNavigating = false;
            }
        }

        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await LoadClientes();
        }
    }
}
