using Gasolutions.Maui.App.Mobal;
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
            _todosLosBarberos = new ObservableCollection<UsuarioModels>();
            _barberosFiltrados = new ObservableCollection<UsuarioModels>();
            RefreshCommand = new Command(async () => await RefreshBarberoList());
            BindingContext = this;
            _ = LoadBarberos();
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

        //private async void OnVerDetallesClicked(object sender, EventArgs e)
        //{
        //    if (sender is Button button && button.CommandParameter is UsuarioModels barbero)
        //    {
        //        var detalles = $"Nombre: {barbero.Nombre}\n" +
        //                       $"Email: {barbero.Email}\n" +
        //                       $"Cédula: {barbero.Cedula}\n" +
        //                       $"Especialidades: {barbero.Especialidades}\n" +
        //                       $"Rol: {barbero.Rol}";

        //        await DisplayAlert("Detalles del Barbero", detalles, "OK");
        //    }
        //}
        private async void OnVerDetallesClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is UsuarioModels barbero)
            {
                var detallesPage = new DetallesBarberoPage(barbero);
                await Navigation.PushModalAsync(detallesPage);
            }
        }
        private async void OnEliminarBarberoClicked(object sender, EventArgs e)
        {
            if (sender is Image image &&
                image.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap &&
                tap.CommandParameter is UsuarioModels barbero)
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

                        await AppUtils.MostrarSnackbar("Barbero eliminado correctamente", Colors.Green, Colors.White);
                    }
                    catch (Exception ex)
                    {
                        await AppUtils.MostrarSnackbar($"Error al eliminar barbero: {ex.Message}", Colors.Red, Colors.White);
                    }
                }
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

    }
}


