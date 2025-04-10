using Gasolutions.Maui.App.Pages;
using Gasolutions.Maui.App.Services;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gasolutions.Maui.App.Pages
{
    public partial class InicioPages : ContentPage
    {
        public InicioPages()
        {
            InitializeComponent();

        }
        private async void MainPage(object sender, EventArgs e)
        {
            var reservationService = App.Current.Handler.MauiContext.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new MainPage(reservationService));
        }
        private async void CitasList(object sender, EventArgs e)
        {
            var reservationService = App.Current.Handler.MauiContext.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new ListaCitas(reservationService));
        }
        private async void BuscarCitas(object sender, EventArgs e)
        {
            var reservationService = App.Current.Handler.MauiContext.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new BuscarPage(reservationService));
        }
        private async void PerfilPage(object sender, EventArgs e)
        {
            //var reservationService = App.Current.Handler.MauiContext.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new PerfilPage());
        }

        private async void OnInicioClicked(object sender, EventArgs e)
        {
            await AnimateButtonClick(sender as Button);
            await Navigation.PushAsync(new InicioPages());
        }

        private async void OnBuscarClicked(object sender, EventArgs e)
        {
            await AnimateButtonClick(sender as Button);
            var reservationService = App.Current.Handler.MauiContext.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new BuscarPage(reservationService));
        }

        private async void OnConfiguracionClicked(object sender, EventArgs e)
        {
            await AnimateButtonClick(sender as Button);
            var reservationService = App.Current.Handler.MauiContext.Services.GetRequiredService<ReservationService>();
            await Navigation.PushAsync(new ListaCitas(reservationService));
        }
        private async Task AnimateButtonClick(Button button)
        {
            if (button == null) return;

            await button.ScaleTo(0.9, 100);
            await button.ScaleTo(1, 100);
        }
    }
}
