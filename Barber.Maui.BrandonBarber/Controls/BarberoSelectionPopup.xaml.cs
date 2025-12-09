using Barber.Maui.BrandonBarber.Models;

namespace Barber.Maui.BrandonBarber.Controls
{
    public partial class BarberoSelectionPopup : ContentPage
    {
        private TaskCompletionSource<UsuarioModels?> _tcs = new();
        private bool _isSelecting = false; // Previene doble clic

        public BarberoSelectionPopup(List<UsuarioModels> barberos)
        {
            InitializeComponent();
            BarberosCollection.ItemsSource = barberos;
        }

        public async Task<UsuarioModels?> ShowAsync()
        {
            await Application.Current.MainPage.Navigation.PushModalAsync(this);
            return await _tcs.Task;
        }

        private async void OnBarberoTapped(object sender, EventArgs e)
        {
            if (_isSelecting) return; // Previene doble clic
            _isSelecting = true;
            try
            {
                if (sender is Border border && border.BindingContext is UsuarioModels barbero)
                {
                    border.IsEnabled = false; // Deshabilita el border visualmente
                    await border.ScaleTo(0.95, 100);
                    await border.ScaleTo(1, 100);

                    _tcs.TrySetResult(barbero);
                    await Application.Current.MainPage.Navigation.PopModalAsync();
                }
            }
            finally
            {
                _isSelecting = false;
            }
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            if (_isSelecting) return; // Previene doble clic en cancelar
            _isSelecting = true;
            try
            {
                _tcs.TrySetResult(null);
                await Application.Current.MainPage.Navigation.PopModalAsync();
            }
            finally
            {
                _isSelecting = false;
            }
        }
    }
}