using System.Collections.ObjectModel;
using Barber.Maui.BrandonBarber.Models;
using Microsoft.Maui.Controls;
using System.Text.Json;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class ListarTodosUsuariosPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly ObservableCollection<UsuarioModels> _todosLosUsuarios;
        private ObservableCollection<UsuarioModels> _usuariosFiltrados;
        private bool _isNavigating = false;
        public Command RefreshCommand { get; }
        public ObservableCollection<UsuarioModels> UsuariosFiltrados
        {
            get => _usuariosFiltrados;
            set { _usuariosFiltrados = value; OnPropertyChanged(); }
        }

        public ListarTodosUsuariosPage()
        {
            InitializeComponent();
            _authService = Application.Current!.Handler.MauiContext!.Services.GetService<AuthService>()!;
            _todosLosUsuarios = [];
            _usuariosFiltrados = [];
            RefreshCommand = new Command(async () => await RefreshUsuariosList());
            BindingContext = this;
            _ = LoadUsuarios();
        }

        private async Task RefreshUsuariosList()
        {
            if (UsuariosRefreshView.IsRefreshing)
            {
                await LoadUsuarios();
                UsuariosRefreshView.IsRefreshing = false;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = LoadUsuarios();
        }

        private async Task LoadUsuarios()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;
                ContentContainer.IsVisible = false;

                var response = await _authService._BaseClient.GetAsync("api/perfiles");
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var usuarios = JsonSerializer.Deserialize<List<UsuarioModels>>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    _todosLosUsuarios.Clear();
                    _usuariosFiltrados.Clear();
                    foreach (var usuario in usuarios ?? [])
                    {
                        _todosLosUsuarios.Add(usuario);
                        _usuariosFiltrados.Add(usuario);
                    }
                    UpdateStats();
                }
                else
                {
                    await AppUtils.MostrarSnackbar("No se pudieron cargar los usuarios", Colors.Red, Colors.White);
                }
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
                ContentContainer.IsVisible = true;
                EmptyStateFrame.IsVisible = !_usuariosFiltrados.Any();
            }
        }

        private void UpdateStats()
        {
            TotalUsuariosLabel.Text = _todosLosUsuarios.Count.ToString();
            UsuariosActivosLabel.Text = _todosLosUsuarios.Count.ToString();
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = e.NewTextValue?.ToLower() ?? string.Empty;
            _usuariosFiltrados.Clear();
            var filtered = string.IsNullOrWhiteSpace(searchText)
            ? _todosLosUsuarios
            : _todosLosUsuarios.Where(u =>
            (u.Nombre ?? "").ToLower().Contains(searchText) ||
            (u.Email ?? "").ToLower().Contains(searchText) ||
            (u.Rol ?? "").ToLower().Contains(searchText) ||
            u.Cedula.ToString().Contains(searchText));
            foreach (var usuario in filtered)
                _usuariosFiltrados.Add(usuario);
            EmptyStateFrame.IsVisible = !_usuariosFiltrados.Any();
        }

        private async void OnVerDetallesClicked(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                if (sender is Button button && button.CommandParameter is UsuarioModels usuario)
                {
                    var detallesPage = new DetalleAdminPage(usuario);
                    await Navigation.PushModalAsync(detallesPage);
                }
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private async void OnEliminarUsuarioClicked(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                if (sender is Image image &&
                image.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap &&
                tap.CommandParameter is UsuarioModels usuario)
                {
                    var popup = new CustomAlertPopup($"¿Está seguro de que desea eliminar a {usuario.Nombre}?");
                    bool confirm = await popup.ShowAsync(this);
                    if (confirm)
                    {
                        try
                        {
                            await _authService.EliminarUsuario(usuario.Cedula);
                            _todosLosUsuarios.Remove(usuario);
                            _usuariosFiltrados.Remove(usuario);
                            UpdateStats();
                            EmptyStateFrame.IsVisible = !_usuariosFiltrados.Any();
                            await AppUtils.MostrarSnackbar("Usuario eliminado correctamente", Colors.Green, Colors.White);
                        }
                        catch (Exception ex)
                        {
                            await AppUtils.MostrarSnackbar($"Error al eliminar usuario: {ex.Message}", Colors.Red, Colors.White);
                        }
                    }
                }
            }
            finally
            {
                _isNavigating = false;
            }
        }
    }
}
