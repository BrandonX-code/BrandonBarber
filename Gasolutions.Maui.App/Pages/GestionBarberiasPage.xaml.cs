using Gasolutions.Maui.App.Mobal;
using Gasolutions.Maui.App.Models;
using Gasolutions.Maui.App.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Gasolutions.Maui.App.Pages
{
    public partial class GestionBarberiasPage : ContentPage, INotifyPropertyChanged
    {
        private long _idAdministrador;
        private readonly BarberiaService _barberiaService;

        public ObservableCollection<Barberia> Barberias { get; } = new ObservableCollection<Barberia>();
        public ObservableCollection<Barberia> FilteredBarberias { get; } = new ObservableCollection<Barberia>();

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

        public ICommand LoadBarberiasCommand { get; }
        public ICommand AgregarBarberiaCommand { get; }
        public ICommand EditarBarberiaCommand { get; }
        public ICommand EliminarBarberiaCommand { get; }
        public ICommand ClearSearchCommand { get; }

        public GestionBarberiasPage()
        {
            InitializeComponent();
            _barberiaService = Application.Current.Handler.MauiContext.Services.GetService<BarberiaService>();

            BindingContext = this;
            _idAdministrador = AuthService.CurrentUser.Cedula;
            LoadBarberiasCommand = new Command(async () => await LoadBarberias());
            AgregarBarberiaCommand = new Command(async () => await AgregarBarberia());
            EditarBarberiaCommand = new Command<Barberia>(async (barberia) => await EditarBarberia(barberia));
            EliminarBarberiaCommand = new Command<Barberia>(async (barberia) => await EliminarBarberia(barberia));
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
                var barberias = await _barberiaService.GetBarberiasByAdministradorAsync(_idAdministrador);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Barberias.Clear();
                    foreach (var barberia in barberias)
                    {
                        Barberias.Add(barberia);
                    }
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
                FilteredBarberias.Clear();

                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    foreach (var barberia in Barberias)
                    {
                        FilteredBarberias.Add(barberia);
                    }
                }
                else
                {
                    var searchLower = SearchText.ToLowerInvariant();
                    var filtered = Barberias.Where(b =>
                        (b.Nombre?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                        (b.Direccion?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                        (b.Telefono?.ToLowerInvariant().Contains(searchLower) ?? false)
                    ).ToList();

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

        private bool _isProcessingClick;
        private async void AgregarButton_Clicked(object sender, EventArgs e)
        {
            if (_isProcessingClick) return;
            _isProcessingClick = true;

            try
            {
                if (sender is Button button) button.IsEnabled = false;
                await AgregarBarberia();
            }
            finally
            {
                _isProcessingClick = false;
                if (sender is Button button) button.IsEnabled = true;
            }
        }

        private async Task AgregarBarberia()
        {
            try
            {
                if (Navigation == null)
                {
                    await DisplayAlert("Error", "No se puede navegar en este momento", "OK");
                    return;
                }

                var formPage = new FormBarberiaPage();
                formPage.BarberiaGuardada += OnBarberiaGuardada;

                await Device.InvokeOnMainThreadAsync(async () =>
                {
                    await Navigation.PushAsync(formPage);
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al abrir formulario: {ex.Message}", "OK");
            }
        }

        private async Task EditarBarberia(Barberia barberia)
        {
            if (barberia == null) return;

            try
            {
                var formPage = new FormBarberiaPage(barberia);
                formPage.BarberiaGuardada += OnBarberiaGuardada;
                await Navigation.PushAsync(formPage);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al abrir formulario: {ex.Message}", "OK");
            }
        }

        private async Task EliminarBarberia(Barberia barberia)
        {
            if (barberia == null) return;
            var popup = new CustomAlertPopup($"¿Quieres Eliminar La Barbería '{barberia.Nombre}'?");
            bool confirmar = await popup.ShowAsync(this);

            if (!confirmar) return;

            try
            {
                IsBusy = true;
                bool success = await _barberiaService.DeleteBarberiaAsync(barberia.Idbarberia);

                if (success)
                {
                    await DisplayAlert("Éxito", "Barbería eliminada correctamente", "OK");
                    await LoadBarberias();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al eliminar barbería: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void OnBarberiaGuardada(object sender, EventArgs e)
        {
            await LoadBarberias();
        }

        private void OnEditarBtnClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Barberia barberia)
            {
                if (EditarBarberiaCommand?.CanExecute(barberia) ?? false)
                {
                    EditarBarberiaCommand.Execute(barberia);
                }
            }
        }

        private void OnEliminarBtnClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Barberia barberia)
            {
                if (EliminarBarberiaCommand?.CanExecute(barberia) ?? false)
                {
                    EliminarBarberiaCommand.Execute(barberia);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}