using Gasolutions.Maui.App.Models;
using Gasolutions.Maui.App.Services;
using System.Collections.ObjectModel;

namespace Gasolutions.Maui.App.Pages
{
    public partial class GestionarDisponibilidadPage : ContentPage
    {
        private readonly DisponibilidadService _disponibilidadService;
        private readonly ReservationService _reservationService;
        private DateTime _selectedDate;
        private ObservableCollection<CitaModel> _citas;
        private Dictionary<string, bool> _horariosDisponibles;

        public DateTime MinimumDate => DateTime.Today;

        public GestionarDisponibilidadPage(DisponibilidadService disponibilidadService, ReservationService reservationService)
        {
            InitializeComponent();
            _disponibilidadService = disponibilidadService;
            _reservationService = reservationService;
            _selectedDate = DateTime.Today;
            _citas = new ObservableCollection<CitaModel>();
            _horariosDisponibles = new Dictionary<string, bool>();

            FechaSelector.Date = _selectedDate;
            CitasCollection.ItemsSource = _citas;
            this.BindingContext = this;

            // Inicializar horarios disponibles y cargar datos
            InitializeHorarios();
            LoadData();
        }

        private void InitializeHorarios()
        {
            _horariosDisponibles = new Dictionary<string, bool>
            {
                { "9:00", false },
                { "10:00", false },
                { "11:00", false },
                { "14:00", false },
                { "15:00", false }
            };
        }

        private async void LoadData()
        {
            await LoadCitas();
            await LoadDisponibilidad();
        }

        private async Task LoadCitas()
        {
            try
            {
                var citas = await _reservationService.GetReservations(_selectedDate);
                _citas.Clear();
                foreach (var cita in citas)
                {
                    _citas.Add(cita);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudieron cargar las citas: {ex.Message}", "Aceptar");
            }
        }

        private async Task LoadDisponibilidad()
        {
            try
            {
                // Resetear checkboxes
                Horario9.IsChecked = false;
                Horario10.IsChecked = false;
                Horario11.IsChecked = false;
                Horario14.IsChecked = false;
                Horario15.IsChecked = false;

                // Cargar disponibilidad para la fecha seleccionada
                var disponibilidad = await _disponibilidadService.GetDisponibilidad(_selectedDate, AuthService.CurrentUser.BarberoId);

                if (disponibilidad != null && disponibilidad.Horarios != null)
                {
                    _horariosDisponibles = disponibilidad.Horarios;

                    // Actualizar checkboxes
                    Horario9.IsChecked = _horariosDisponibles.ContainsKey("9:00") && _horariosDisponibles["9:00"];
                    Horario10.IsChecked = _horariosDisponibles.ContainsKey("10:00") && _horariosDisponibles["10:00"];
                    Horario11.IsChecked = _horariosDisponibles.ContainsKey("11:00") && _horariosDisponibles["11:00"];
                    Horario14.IsChecked = _horariosDisponibles.ContainsKey("14:00") && _horariosDisponibles["14:00"];
                    Horario15.IsChecked = _horariosDisponibles.ContainsKey("15:00") && _horariosDisponibles["15:00"];
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo cargar la disponibilidad: {ex.Message}", "Aceptar");
            }
        }

        private async void OnDateSelected(object sender, DateChangedEventArgs e)
        {
            _selectedDate = e.NewDate;
            LoadData();
        }

        private void OnHorarioCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                string hora = "";

                // Identificar la hora según el checkbox
                if (checkBox == Horario9) hora = "9:00";
                else if (checkBox == Horario10) hora = "10:00";
                else if (checkBox == Horario11) hora = "11:00";
                else if (checkBox == Horario14) hora = "14:00";
                else if (checkBox == Horario15) hora = "15:00";

                // Actualizar el diccionario
                if (!string.IsNullOrEmpty(hora))
                {
                    _horariosDisponibles[hora] = checkBox.IsChecked;
                }

                // Verificar si hay citas existentes para este horario
                VerificarConflictos(hora, checkBox.IsChecked);
            }
        }

        private async void VerificarConflictos(string hora, bool disponible)
        {
            if (!disponible)
            {
                // Verificar si hay citas programadas para este horario
                var horaDateTime = DateTime.Parse(hora);
                var citasAfectadas = _citas.Where(c => c.Fecha.Hour == horaDateTime.Hour).ToList();

                if (citasAfectadas.Any())
                {
                    bool confirmar = await DisplayAlert("Atención",
                        "Hay citas programadas para este horario. Si lo marca como no disponible, estas citas se cancelarán. ¿Desea continuar?",
                        "Sí", "No");

                    if (!confirmar)
                    {
                        // Revertir el cambio
                        _horariosDisponibles[hora] = true;
                        ActualizarCheckbox(hora, true);
                    }
                }
            }
        }

        private void ActualizarCheckbox(string hora, bool estado)
        {
            switch (hora)
            {
                case "9:00": Horario9.IsChecked = estado; break;
                case "10:00": Horario10.IsChecked = estado; break;
                case "11:00": Horario11.IsChecked = estado; break;
                case "14:00": Horario14.IsChecked = estado; break;
                case "15:00": Horario15.IsChecked = estado; break;
            }
        }

        // En GestionarDisponibilidadPage.xaml.cs, modifica el método OnGuardarClicked
        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            try
            {
                var disponibilidad = new DisponibilidadModel
                {
                    Id = 0,
                    Fecha = DateTime.Today,
                    BarberoId = 1, // <--- PON UN ID MAYOR A CERO
                    Horarios = new Dictionary<string, bool>
                    {
                        { "09:00", true },
                        { "10:00", false },
                        { "11:00", true }
                    }
                };


                // Guardar disponibilidad
                bool result = await _disponibilidadService.GuardarDisponibilidad(disponibilidad);

                if (result)
                {
                    await DisplayAlert("Éxito", "La disponibilidad ha sido guardada correctamente", "Aceptar");
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo guardar la disponibilidad", "Aceptar");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al guardar: {ex.Message}", "Aceptar");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}