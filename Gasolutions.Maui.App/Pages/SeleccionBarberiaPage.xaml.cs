using System.Runtime.CompilerServices;

namespace Gasolutions.Maui.App.Pages
{
    public partial class SeleccionBarberiaPage : ContentPage, INotifyPropertyChanged
    {
        private readonly BarberiaService _barberiaService;

        private ObservableCollection<Barberia> _barberias = new ObservableCollection<Barberia>();
        public ObservableCollection<Barberia> Barberias
        {
            get => _barberias;
            set
            {
                _barberias = value;
                OnPropertyChanged();
                FilterBarberias();
            }
        }

        private ObservableCollection<Barberia> _filteredBarberias = new ObservableCollection<Barberia>();
        public ObservableCollection<Barberia> FilteredBarberias
        {
            get => _filteredBarberias;
            set
            {
                _filteredBarberias = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasResults));
                OnPropertyChanged(nameof(EmptyMessage));
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
                FilterBarberias();
            }
        }
        public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);
        public bool HasResults => FilteredBarberias?.Count > 0;
        public string EmptyMessage => string.IsNullOrWhiteSpace(SearchText)
            ? "No hay barberías registradas"
            : $"No se encontraron barberías que coincidan con '{SearchText}'";

        public event EventHandler<Barberia>? BarberiaSeleccionada;
        public Command ClearSearchCommand { get; }

        public SeleccionBarberiaPage()
        {
            InitializeComponent();
            _barberiaService = Application.Current!.Handler.MauiContext!.Services.GetService<BarberiaService>()!;
            BindingContext = this;
            ClearSearchCommand = new Command(ClearSearch);
            PropertyChanged = delegate { }; // Initialize PropertyChanged to avoid null warnings
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (!FilteredBarberias.Any())
            {
                await LoadBarberias();
            }
        }

        private async Task LoadBarberias()
        {
            try
            {
                var barberias = await _barberiaService.GetBarberiasAsync();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Barberias.Clear();

                    if (barberias?.Any() == true)
                    {
                        foreach (var barberia in barberias)
                        {
                            // Asegurar que la URL del logo esté correcta
                            if (string.IsNullOrWhiteSpace(barberia.LogoUrl))
                            {
                                barberia.LogoUrl = "picture.png"; // Imagen por defecto
                            }
                            Barberias.Add(barberia);
                        }
                    }

                    FilterBarberias();
                });
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await AppUtils.MostrarSnackbar($"Error cargando barberías: {ex.Message}", Colors.Red, Colors.White);
                });
            }
        }

        private void FilterBarberias()
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var sourceList = Barberias?.ToList() ?? new List<Barberia>();

                    if (string.IsNullOrWhiteSpace(SearchText))
                    {
                        // Mostrar todas las barberías
                        UpdateFilteredList(sourceList);
                    }
                    else
                    {
                        // Filtrar por múltiples campos
                        var searchLower = SearchText.Trim().ToLowerInvariant();
                        var filtered = sourceList.Where(b =>
                            ContainsIgnoreCase(b.Nombre, searchLower) ||
                            ContainsIgnoreCase(b.Email, searchLower) ||
                            ContainsIgnoreCase(b.Telefono, searchLower) ||
                            ContainsIgnoreCase(b.Direccion, searchLower)
                        ).ToList();

                        UpdateFilteredList(filtered);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error filtering barberias: {ex.Message}");
            }
        }

        private void UpdateFilteredList(List<Barberia> newList)
        {
            FilteredBarberias.Clear();
            foreach (var barberia in newList)
            {
                FilteredBarberias.Add(barberia);
            }
        }

        private static bool ContainsIgnoreCase(string source, string search)
        {
            return !string.IsNullOrEmpty(source) &&
                   source.ToLowerInvariant().Contains(search);
        }

        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

        private async void OnBarberiaTapped(object sender, EventArgs e)
        {
            try
            {
                if (sender is Border frame && frame.BindingContext is Barberia selectedBarberia)
                {
                    // Feedback visual (opcional)
                    frame.BackgroundColor = Colors.LightGray;
                    await Task.Delay(100);
                    frame.BackgroundColor = Color.FromArgb("#1E3A49");

                    // Disparar evento y cerrar página
                    BarberiaSeleccionada?.Invoke(this, selectedBarberia);
                    await Navigation.PopAsync();
                }
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error seleccionando barbería: {ex.Message}", Colors.Red, Colors.White);
            }
        }

        public new PropertyChangedEventHandler PropertyChanged;
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}