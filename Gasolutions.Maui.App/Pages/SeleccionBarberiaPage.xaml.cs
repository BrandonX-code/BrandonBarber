using Gasolutions.Maui.App.Models;
using Gasolutions.Maui.App.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Gasolutions.Maui.App.Pages
{
    public partial class SeleccionBarberiaPage : ContentPage, INotifyPropertyChanged
    {
        private readonly BarberiaService _barberiaService;

        private ObservableCollection<Barberia> _barberias = new();
        public ObservableCollection<Barberia> Barberias
        {
            get => _barberias;
            set
            {
                _barberias = value;
                OnPropertyChanged();
                FilterBarberias(); // Aplicar filtro cuando cambie la lista
            }
        }

        private ObservableCollection<Barberia> _filteredBarberias = new();
        public ObservableCollection<Barberia> FilteredBarberias
        {
            get => _filteredBarberias;
            set
            {
                _filteredBarberias = value;
                OnPropertyChanged();
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSearchText));
                FilterBarberias(); // Filtrar cada vez que cambie el texto
            }
        }

        public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);

        public event EventHandler<Barberia> BarberiaSeleccionada;
        public ICommand LoadBarberiasCommand { get; }
        public ICommand ClearSearchCommand { get; }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        public SeleccionBarberiaPage()
        {
            InitializeComponent();
            _barberiaService = Application.Current.Handler.MauiContext.Services.GetService<BarberiaService>();
            BindingContext = this;
            LoadBarberiasCommand = new Command(async () => await LoadBarberias());
            ClearSearchCommand = new Command(ClearSearch);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadBarberias();
        }

        private async Task LoadBarberias()
        {
            IsBusy = true;
            try
            {
                var barberias = await _barberiaService.GetBarberiasAsync();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Barberias.Clear();
                    foreach (var barberia in barberias)
                    {
                        Barberias.Add(barberia);
                    }
                    // Asegurar que FilteredBarberias se actualice después de cargar
                    FilterBarberias();
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error cargando barberías: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void FilterBarberias()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    // Si no hay texto de búsqueda, mostrar todas las barberías
                    FilteredBarberias.Clear();
                    foreach (var barberia in Barberias)
                    {
                        FilteredBarberias.Add(barberia);
                    }
                }
                else
                {
                    // Filtrar por nombre o dirección (sin distinguir mayúsculas/minúsculas)
                    var searchLower = SearchText.ToLowerInvariant();
                    var filtered = Barberias.Where(b =>
                        (b.Nombre?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                        (b.Email?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                        (b.Telefono?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                        (b.Direccion?.ToLowerInvariant().Contains(searchLower) ?? false)
                    ).ToList();

                    FilteredBarberias.Clear();
                    foreach (var barberia in filtered)
                    {
                        FilteredBarberias.Add(barberia);
                    }
                }
            });
        }

        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

        private void OnBarberiaTapped(object sender, EventArgs e)
        {
            if (sender is Frame frame && frame.BindingContext is Barberia selectedBarberia)
            {
                // 1. Dispara el evento
                BarberiaSeleccionada?.Invoke(this, selectedBarberia);
                // 2. Cierra la página CORRECTAMENTE
                Navigation.PopAsync(); // Cambiado a PopAsync (no modal)
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}