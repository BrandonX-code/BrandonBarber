namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class DetalleImagenPage : ContentPage
    {
        private readonly ImagenGaleriaModel _imagen;
        private readonly string? _imageUrl;
        private readonly string _baseUrl;

        private bool _isZoomed = false;
        private double _xOffset, _yOffset;

        public DetalleImagenPage(ImagenGaleriaModel imagen, string baseUrl)
        {
            InitializeComponent();
            _imagen = imagen;
            _baseUrl = baseUrl;

            _imageUrl = imagen.RutaArchivo;
            DetalleImagen.Source = ImageSource.FromUri(new Uri(_imageUrl!));

            EditarButton.IsVisible = AuthService.CurrentUser?.Rol?.ToLower() == "barbero";

            if (!string.IsNullOrEmpty(imagen.Descripcion))
                Title = imagen.Descripcion;
        }

        private async void OnVolverClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnEditarClicked(object sender, EventArgs e)
        {
            string nuevaDescripcion = await DisplayPromptAsync(
                "Editar descripción",
                "Modifica la descripción de la imagen:",
                initialValue: _imagen.Descripcion ?? "",
                maxLength: 500
            );

            if (nuevaDescripcion == null)
                return;

            var galeriaService = Application.Current!.Handler.MauiContext!.Services.GetService<GaleriaService>();
            bool actualizado = await galeriaService!.ActualizarImagen(_imagen.Id, nuevaDescripcion);

            if (actualizado)
            {
                await AppUtils.MostrarSnackbar("Descripción actualizada.", Colors.Green, Colors.White);
                _imagen.Descripcion = nuevaDescripcion;
            }
            else
            {
                await AppUtils.MostrarSnackbar("No se pudo actualizar la descripción.", Colors.Red, Colors.White);
            }
        }

        private async void OnCompartirClicked(object sender, EventArgs e)
        {
            try
            {
                string? localFilePath = await DownloadImageToTempFile(_imageUrl!);
                if (string.IsNullOrEmpty(localFilePath))
                {
                    await AppUtils.MostrarSnackbar("No se pudo descargar la imagen para compartir.", Colors.Red, Colors.White);
                    return;
                }

                await Share.RequestAsync(new ShareFileRequest
                {
                    Title = "Compartir corte de cabello",
                    File = new ShareFile(localFilePath)
                });
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"No se pudo compartir la imagen: {ex.Message}", Colors.Red, Colors.White);
            }
        }

        private async static Task<string?> DownloadImageToTempFile(string imageUrl)
        {
            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(imageUrl);
                response.EnsureSuccessStatusCode();

                var imageBytes = await response.Content.ReadAsByteArrayAsync();

                string fileName = System.IO.Path.GetFileName(new Uri(imageUrl).LocalPath);
                string tempFilePath = System.IO.Path.Combine(FileSystem.CacheDirectory, fileName);

                await File.WriteAllBytesAsync(tempFilePath, imageBytes);
                return tempFilePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error downloading image for sharing: {ex.Message}");
                return null;
            }
        }

        // 👇 Doble toque: activar/desactivar zoom
        private async void OnImageDoubleTapped(object sender, TappedEventArgs e)
        {
            if (_isZoomed)
            {
                await DetalleImagen.ScaleTo(1, 250, Easing.CubicOut);
                await DetalleImagen.TranslateTo(0, 0, 250, Easing.CubicOut);
                _isZoomed = false;
                _xOffset = _yOffset = 0;
            }
            else
            {
                DetalleImagen.AnchorX = 0.5;
                DetalleImagen.AnchorY = 0.5;
                await DetalleImagen.ScaleTo(2, 250, Easing.CubicOut);
                _isZoomed = true;
            }
        }

        // 👇 Permitir arrastrar cuando está en zoom
        private void OnImagePanned(object sender, PanUpdatedEventArgs e)
        {
            if (!_isZoomed)
                return;

            switch (e.StatusType)
            {
                case GestureStatus.Running:
                    DetalleImagen.TranslationX = _xOffset + e.TotalX;
                    DetalleImagen.TranslationY = _yOffset + e.TotalY;
                    break;

                case GestureStatus.Completed:
                    _xOffset = DetalleImagen.TranslationX;
                    _yOffset = DetalleImagen.TranslationY;
                    break;
            }
        }
    }
}
