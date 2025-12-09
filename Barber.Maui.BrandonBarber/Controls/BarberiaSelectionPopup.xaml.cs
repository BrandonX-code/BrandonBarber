using Barber.Maui.BrandonBarber.Models;

namespace Barber.Maui.BrandonBarber.Controls
{
    public partial class BarberiaSelectionPopup : ContentPage
    {
        private TaskCompletionSource<Barberia?> _tcs = new();
        private bool _isSelecting = false;

        public BarberiaSelectionPopup(List<Barberia> barberias)
        {
            InitializeComponent();
            BarberiasCollection.ItemsSource = barberias;
        }

        public async Task<Barberia?> ShowAsync()
        {
            await Application.Current.MainPage.Navigation.PushModalAsync(this);
            return await _tcs.Task;
        }

        private async void OnBarberiaTapped(object sender, EventArgs e)
        {
            if (_isSelecting) return;
            _isSelecting = true;
            try
            {
                if (sender is Border border && border.BindingContext is Barberia barberia)
                {
                    border.IsEnabled = false;
                    await border.ScaleTo(0.95, 100);
                    await border.ScaleTo(1, 100);

                    _tcs.TrySetResult(barberia);
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
            if (_isSelecting) return;
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