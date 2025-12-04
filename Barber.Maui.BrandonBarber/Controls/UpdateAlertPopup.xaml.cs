using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace Barber.Maui.BrandonBarber.Controls
{
    public partial class UpdateAlertPopup : ContentPage
    {
        private string _apkUrl = "";
        private TaskCompletionSource<bool> _tcs = new();

        public UpdateAlertPopup(string mensaje, string apkUrl)
        {
            InitializeComponent();
            MessageLabel.Text = mensaje;
            _apkUrl = apkUrl;
        }

        public async Task<bool> ShowAsync()
        {
            await Application.Current.MainPage.Navigation.PushModalAsync(this);
            return await _tcs.Task;
        }

        private async void OnUpdateClicked(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_apkUrl))
                {
                    await Launcher.OpenAsync(_apkUrl);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"No se pudo abrir el enlace: {ex.Message}", "OK");
            }
            _tcs.TrySetResult(true);
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            _tcs.TrySetResult(false);
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }
    }
}
