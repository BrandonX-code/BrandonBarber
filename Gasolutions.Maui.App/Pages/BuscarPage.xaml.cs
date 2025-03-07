using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Gasolutions.Maui.App.Services;
using Gasolutions.Maui.App.Models;

namespace Gasolutions.Maui.App.Pages
{
    public partial class BuscarPage : ContentPage
    {
        public ObservableCollection<CitaModel> CitasFiltradas { get; set; } = new();
        private readonly ReservationService _reservationService;
        public BuscarPage(ReservationService reservationService)
        {
            InitializeComponent();
            BindingContext = this;
            ResultadosCollection.ItemsSource = CitasFiltradas;
            _reservationService = reservationService;
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        private async void OnSearchClicked(object sender, EventArgs e)
        {
            await ActualizarLista();
        }

        private async Task ActualizarLista()
        {
            CitasFiltradas.Clear();

            if (string.IsNullOrWhiteSpace(SearchEntry.Text) || !long.TryParse(SearchEntry.Text, out long id))
            {
                await DisplayAlert("Error", "Ingrese un ID válido.", "Aceptar");
                return;
            }

            var cita = await _reservationService.GetReservationsById(id);

            if (cita == null)
            {
                await DisplayAlert("Error", "No se encontró ninguna cita con ese ID.", "Aceptar");
                return;
            }

            CitasFiltradas.Add(cita);
        }


        private void OnClearClicked(object sender, EventArgs e)
        {
            SearchEntry.Text = string.Empty;
            FechaPicker.Date = DateTime.Today;
            CitasFiltradas.Clear();
        }
    }

}

    

