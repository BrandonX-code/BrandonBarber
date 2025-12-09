namespace Barber.Maui.BrandonBarber.Controls
{
    public partial class ServicioSelectionPopup : ContentPage
    {
        private TaskCompletionSource<ServicioModel?> _tcs = new();

        public ServicioSelectionPopup(List<ServicioModel> servicios)
        {
            InitializeComponent();
            ServiciosCollection.ItemsSource = servicios;
        }

        public async Task<ServicioModel?> ShowAsync()
        {
            await Application.Current.MainPage.Navigation.PushModalAsync(this);
            return await _tcs.Task;
        }

        private async void OnServicioTapped(object sender, EventArgs e)
        {
            if (sender is Border border && border.BindingContext is ServicioModel servicio)
            {
                await border.ScaleTo(0.95, 100);
                await border.ScaleTo(1, 100);

                _tcs.TrySetResult(servicio);
                await Application.Current.MainPage.Navigation.PopModalAsync();
            }
        }

        private void OnServicioSelected(object sender, SelectionChangedEventArgs e)
        {
            // Opcional: manejar selección
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            _tcs.TrySetResult(null);
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }
    }
}