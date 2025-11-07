using System;
using System.Collections.ObjectModel;
using Barber.Maui.BrandonBarber.Models;
using Microsoft.Maui.Controls;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class GestionarAdministradoresPage : ContentPage
    {
        private readonly AdministradorService _adminService;
        private readonly ObservableCollection<UsuarioModels> _todosLosAdmins;
        private ObservableCollection<UsuarioModels> _adminsFiltrados;
        private bool _isNavigating = false;
        public Command RefreshCommand { get; }
        //public ObservableCollection<UsuarioModels> AdminsFiltrados
        //{
        //    get => _adminsFiltrados;
        //    set { _adminsFiltrados = value; OnPropertyChanged(); }
        //}
        public ObservableCollection<UsuarioModels> AdminsFiltrados
        {
            get => _adminsFiltrados;
            set
            {
                _adminsFiltrados = value;
                OnPropertyChanged();
            }
        }
        public GestionarAdministradoresPage()
        {
            InitializeComponent();
            _adminService = Application.Current!.Handler.MauiContext!.Services.GetService<AdministradorService>()!;
            _todosLosAdmins = [];
            _adminsFiltrados = [];
            RefreshCommand = new Command(async () => await RefreshAdminList());
            BindingContext = this;
            _ = LoadAdmins();
        }

        private async Task RefreshAdminList()
        {
            if (AdminRefreshView.IsRefreshing)
            {
                await LoadAdmins();
                AdminRefreshView.IsRefreshing = false;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = LoadAdmins();
        }

        private async Task LoadAdmins()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;
                ContentContainer.IsVisible = false;

                var lista = await _adminService.GetAdministradoresAsync();
                _todosLosAdmins.Clear();
                _adminsFiltrados.Clear();
                foreach (var admin in lista)
                {
                    _todosLosAdmins.Add(admin);
                    _adminsFiltrados.Add(admin);
                }
                UpdateStats();
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
                ContentContainer.IsVisible = true;
                EmptyStateFrame.IsVisible = !_adminsFiltrados.Any();
            }
        }

        private void UpdateStats()
        {
            TotalAdminsLabel.Text = _todosLosAdmins.Count.ToString();
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = e.NewTextValue?.ToLower() ?? string.Empty;
            _adminsFiltrados.Clear();
            var filtered = string.IsNullOrWhiteSpace(searchText)
                ? _todosLosAdmins
                : _todosLosAdmins.Where(a =>
                    (a.Nombre ?? "").ToLower().Contains(searchText) ||
                    (a.Email ?? "").ToLower().Contains(searchText) ||
                    a.Cedula.ToString().Contains(searchText));
            foreach (var admin in filtered)
                _adminsFiltrados.Add(admin);
            EmptyStateFrame.IsVisible = !_adminsFiltrados.Any();
        }
        private async void OnVerDetallesClicked(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                if (sender is Button button && button.CommandParameter is UsuarioModels barbero)
                {
                    var detallesPage = new DetalleAdminPage(barbero);
                    await Navigation.PushModalAsync(detallesPage);
                }
            }
            finally
            {
                _isNavigating = false;
            }
        }
    }
}
