using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Gasolutions.Maui.App.Services;

namespace Gasolutions.Maui.App.Pages
{
    public partial class BuscarPage : ContentPage
    {
        public ObservableCollection<Cita> CitasFiltradas { get; set; } = new();

        public BuscarPage()
        {
            InitializeComponent();
            ResultadosCollection.ItemsSource = CitasFiltradas;
        }

        private void OnSearchClicked(object sender, EventArgs e)
        {
            string? searchText = SearchEntry.Text?.ToLower();
            DateTime? fechaSeleccionada = FechaPicker.Date;
            var citas = ObtenerCitas();
            var resultados = citas.Where(c =>
                (string.IsNullOrEmpty(searchText) ||
                 c.Nombre.ToLower().Contains(searchText) ||
                 c.Id.ToString().Contains(searchText)) &&
                (fechaSeleccionada == null || c.Fecha.Date == fechaSeleccionada.Value.Date))
                .ToList();
            CitasFiltradas.Clear();
            foreach (var cita in resultados)
            {
                CitasFiltradas.Add(cita);
            }
        }

        private void OnClearClicked(object sender, EventArgs e)
        {
            SearchEntry.Text = string.Empty;
            FechaPicker.Date = DateTime.Today;
            CitasFiltradas.Clear();
        }

        private List<Cita> ObtenerCitas()
        {
            // Now retrieves reservations from the ReservationService
            return ReservationService.GetReservations();
        }
    }

    
}
