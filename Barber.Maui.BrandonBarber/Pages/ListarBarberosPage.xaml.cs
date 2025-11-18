namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class ListarBarberosPage : ContentPage
    {
        public readonly AuthService _authService;
        private readonly ObservableCollection<UsuarioModels> _todosLosBarberos;
        private ObservableCollection<UsuarioModels> _barberosFiltrados;
        private readonly BarberiaService? _barberiaService;
        private List<Barberia>? _barberias;
        private int? _barberiaSeleccionadaId = null;
        private bool _isNavigating = false;
        public Command RefreshCommand { get; }
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
            _barberiaService = Application.Current!.Handler.MauiContext!.Services.GetService<BarberiaService>();
            _todosLosBarberos = [];
            _barberosFiltrados = [];
            RefreshCommand = new Command(async () => await RefreshBarberoList());
            BindingContext = this;

            CargarBarberias();
            _ = LoadBarberos();
        }
        private async void CargarBarberias()
        {
            try
            {
                long idAdministrador = AuthService.CurrentUser!.Cedula;
                _barberias = await _barberiaService!.GetBarberiasByAdministradorAsync(idAdministrador);

                BarberiaPicker.ItemsSource = _barberias;
                PickerSection.IsVisible = _barberias.Any();

                // No seleccionar nada por defecto, quedará vacío
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudieron cargar las barberías: " + ex.Message, "OK");
            }
        }

        private async void BarberiaPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            var picker = (Picker)sender;
            int selectedIndex = picker.SelectedIndex;
            if (selectedIndex != -1)
            {
                var barberiaSeleccionada = (Barberia)picker.SelectedItem;
                _barberiaSeleccionadaId = barberiaSeleccionada.Idbarberia;
                await LoadBarberos();
            }
        }

        private async Task RefreshBarberoList()
        {
            if (BarberoRefreshView.IsRefreshing)
            {
                await LoadBarberos();
                BarberoRefreshView.IsRefreshing = false;
            }
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = LoadBarberos();
        }

        private async Task LoadBarberos()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                ContentContainer.IsVisible = false;

                var response = await _authService._BaseClient.GetAsync("api/auth");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var usuarios = System.Text.Json.JsonSerializer.Deserialize<List<UsuarioModels>>(jsonContent,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var admin = AuthService.CurrentUser;
                    // Si no hay barbería seleccionada, no mostrar barberos
                    if (_barberiaSeleccionadaId == null)
                    {
                        _todosLosBarberos.Clear();
                        _barberosFiltrados.Clear();
                        UpdateStats();
                        EmptyStateFrame.IsVisible = true;
                        return;
                    }

                    var barberos = usuarios?.Where(u => u.Rol!.ToLower() == "barbero" && u.IdBarberia == _barberiaSeleccionadaId).ToList() ?? [];
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
                    await AppUtils.MostrarSnackbar("No se pudieron cargar los barberos", Colors.Red, Colors.White);
                }
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error al cargar los barberos: {ex.Message}", Colors.Red, Colors.White);
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
                ContentContainer.IsVisible = true;
                EmptyStateFrame.IsVisible = !_barberosFiltrados.Any();
            }
        }

        private void UpdateStats()
        {
            TotalBarberosLabel.Text = _todosLosBarberos.Count.ToString();
        }



        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = e.NewTextValue?.ToLower() ?? string.Empty;

            _barberosFiltrados.Clear();

            var filtered = string.IsNullOrWhiteSpace(searchText)
                ? _todosLosBarberos
                : _todosLosBarberos.Where(b =>
                    b.Nombre!.ToLower().Contains(searchText) ||
                    b.Email!.ToLower().Contains(searchText) ||
                    b.Cedula.ToString().Contains(searchText));

            foreach (var barbero in filtered)
            {
                _barberosFiltrados.Add(barbero);
            }

            EmptyStateFrame.IsVisible = !_barberosFiltrados.Any();
        }
        private async void OnVerDetallesClicked(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                if (sender is Button button && button.CommandParameter is UsuarioModels barbero)
                {
                    var detallesPage = new DetallesBarberoPage(barbero);
                    await Navigation.PushModalAsync(detallesPage);
                }
            }
            finally
            {
                _isNavigating = false;
            }
        }
        private async void OnEliminarBarberoClicked(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                if (sender is Image image &&
                    image.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap &&
                    tap.CommandParameter is UsuarioModels barbero)
                {
                    var popup = new CustomAlertPopup($"¿Está seguro de que desea eliminar al barbero {barbero.Nombre}?");
                    bool confirm = await popup.ShowAsync(this);
                    if (confirm)
                    {
                        try
                        {
                            await _authService.EliminarUsuario(barbero.Cedula);

                            _todosLosBarberos.Remove(barbero);
                            _barberosFiltrados.Remove(barbero);

                            UpdateStats();
                            EmptyStateFrame.IsVisible = !_barberosFiltrados.Any();

                            await AppUtils.MostrarSnackbar("Barbero eliminado correctamente", Colors.Green, Colors.White);
                        }
                        catch (Exception ex)
                        {
                            await AppUtils.MostrarSnackbar($"Error al eliminar barbero: {ex.Message}", Colors.Red, Colors.White);
                        }
                    }
                }
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
                _isNavigating = false;
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

    }
}


