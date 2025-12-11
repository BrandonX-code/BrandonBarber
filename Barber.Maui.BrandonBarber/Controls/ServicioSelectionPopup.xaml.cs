namespace Barber.Maui.BrandonBarber.Controls
{
    public partial class ServicioSelectionPopup : ContentPage
    {
        private TaskCompletionSource<ServicioModel?>? _tcs;
        private bool _isSelecting = false;

        public ServicioSelectionPopup(List<ServicioModel> servicios)
        {
            InitializeComponent();
            // ✅ REINICIALIZAR LA TAREA CADA VEZ QUE SE CREA EL POPUP
            _tcs = new TaskCompletionSource<ServicioModel?>();
            ServiciosCollection.ItemsSource = servicios;
        }

        public async Task<ServicioModel?> ShowAsync()
        {
            if (_tcs == null)
                _tcs = new TaskCompletionSource<ServicioModel?>();

            await Application.Current!.MainPage!.Navigation.PushModalAsync(this);
            return await _tcs.Task;
        }

        private async void OnServicioTapped(object sender, EventArgs e)
        {
            if (_isSelecting) return;
            _isSelecting = true;
            try
            {
                if (sender is Border border && border.BindingContext is ServicioModel servicio)
                {
                    border.IsEnabled = false;
                    await border.ScaleTo(0.95, 100);
                    await border.ScaleTo(1, 100);

                    // ✅ VERIFICAR QUE _tcs NO ES NULL ANTES DE USAR
                    if (_tcs != null && !_tcs.Task.IsCompleted)
                    {
                        _tcs.TrySetResult(servicio);
                    }

                    await Application.Current!.MainPage!.Navigation.PopModalAsync();
                }
            }
            finally
            {
                _isSelecting = false;
            }
        }

        private void OnServicioSelected(object sender, SelectionChangedEventArgs e)
        {
            // Opcional: manejar selección
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            if (_isSelecting) return;
            _isSelecting = true;
            try
            {
                // ✅ VERIFICAR QUE _tcs NO ES NULL ANTES DE USAR
                if (_tcs != null && !_tcs.Task.IsCompleted)
                {
                    _tcs.TrySetResult(null);
                }

                await Application.Current!.MainPage!.Navigation.PopModalAsync();
            }
            finally
            {
                _isSelecting = false;
            }
        }
    }
}