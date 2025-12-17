namespace Barber.Maui.BrandonBarber.Controls
{
    public partial class DescripcionImagenPopup : ContentPage
    {
        private TaskCompletionSource<string?> _tcs = new();
        private bool _isProcessing = false;

        public DescripcionImagenPopup(string initialValue = "")
        {
            InitializeComponent();
            DescripcionEntry.Text = initialValue;
            ActualizarContador();

            DescripcionEntry.TextChanged += (s, e) => ActualizarContador();
        }

        public async Task<string?> ShowAsync()
        {
            await Application.Current.MainPage.Navigation.PushModalAsync(this);
            return await _tcs.Task;
        }

        private void ActualizarContador()
        {
            int caracteresActuales = DescripcionEntry.Text?.Length ?? 0;
            ContadorLabel.Text = $"{caracteresActuales}/500";
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            if (_isProcessing) return;
            _isProcessing = true;
            try
            {
                string descripcion = DescripcionEntry.Text ?? string.Empty;
                _tcs.TrySetResult(descripcion);
                await Application.Current.MainPage.Navigation.PopModalAsync();
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            if (_isProcessing) return;
            _isProcessing = true;
            try
            {
                _tcs.TrySetResult(null);
                await Application.Current.MainPage.Navigation.PopModalAsync();
            }
            finally
            {
                _isProcessing = false;
            }
        }
    }
}