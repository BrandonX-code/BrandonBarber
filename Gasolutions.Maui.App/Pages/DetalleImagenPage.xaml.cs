using Microsoft.Maui.Controls;
using System;
using System.IO;
using System.Threading.Tasks;
using Gasolutions.Maui.App.Models;
using Gasolutions.Maui.App.Services; // Add this using directive for GaleriaService

namespace Gasolutions.Maui.App.Pages
{
    public partial class DetalleImagenPage : ContentPage
    {
        private ImagenGaleriaModel _imagen;
        private string _imageUrl;

        private string _baseUrl;

        public DetalleImagenPage(ImagenGaleriaModel imagen, string baseUrl) 
        {
            InitializeComponent();
            _imagen = imagen;
            _baseUrl = baseUrl;

            _imageUrl = $"{_baseUrl.TrimEnd('/')}{imagen.RutaArchivo}";

            DetalleImagen.Source = ImageSource.FromUri(new Uri(_imageUrl));

            if (!string.IsNullOrEmpty(imagen.Descripcion))
            {
                Title = imagen.Descripcion;
            }
        }

        private async void OnVolverClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnCompartirClicked(object sender, EventArgs e)
        {
            try
            {
                string localFilePath = await DownloadImageToTempFile(_imageUrl);

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

        private async Task<string> DownloadImageToTempFile(string imageUrl)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(imageUrl);
                    response.EnsureSuccessStatusCode();

                    var imageBytes = await response.Content.ReadAsByteArrayAsync();

                    string fileName = Path.GetFileName(new Uri(imageUrl).LocalPath);
                    string tempFilePath = Path.Combine(FileSystem.CacheDirectory, fileName); // Use CacheDirectory for temp files

                    await File.WriteAllBytesAsync(tempFilePath, imageBytes);
                    return tempFilePath;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error downloading image for sharing: {ex.Message}");
                return null;
            }
        }
    }
}