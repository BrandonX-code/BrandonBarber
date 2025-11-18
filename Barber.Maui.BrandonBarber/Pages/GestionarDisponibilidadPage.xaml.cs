namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class GestionarDisponibilidadPage : ContentPage
    {
        private readonly DisponibilidadService _disponibilidadService;
        private readonly ReservationService _reservationService;
        private DateTime _selectedDate;
        private readonly ObservableCollection<CitaModel> _citas;
        private Dictionary<string, bool> _horariosDisponibles;
        private bool _isNavigating = false;
        public static DateTime MinimumDate => DateTime.Today;

        public GestionarDisponibilidadPage(DisponibilidadService disponibilidadService, ReservationService reservationService)
        {
            InitializeComponent();
            _disponibilidadService = disponibilidadService;
            _reservationService = reservationService;
            _selectedDate = DateTime.Today;
            _citas = [];
            _horariosDisponibles = [];
            FechaSelector.MinimumDate = DateTime.Today;
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
                 { "6:00 AM - 12:00 PM", true },
                 { "12:00 PM - 03:00 PM", true },
                 { "03:00 PM - 05:00 PM", true },
                 { "05:00 PM - 07:00 PM", true },
                 { "07:00 PM- 08:00 PM", true }
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
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                long barberoid = AuthService.CurrentUser!.Cedula;
                var citas = await _reservationService.GetReservationsByBarberoAndFecha(barberoid, _selectedDate);
                _citas.Clear();
                foreach (var cita in citas)
                {
                    _citas.Add(cita);
                }
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"No se pudieron cargar las citas: {ex.Message}", Colors.Red, Colors.White);
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }

        private async Task LoadDisponibilidad()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                // Resetear checkboxes
                Horario6a12.IsChecked = false;
                Horario12a3.IsChecked = false;
                Horario3a5.IsChecked = false;
                Horario5a7.IsChecked = false;
                Horario7a8.IsChecked = false;

                // Cargar disponibilidad para la fecha seleccionada
                var barberoId = AuthService.CurrentUser?.Cedula ?? 0;
                var disponibilidad = await _disponibilidadService.GetDisponibilidad(_selectedDate, barberoId);

                if (disponibilidad != null && disponibilidad.Horarios != null)
                {
                    _horariosDisponibles = disponibilidad.HorariosDict;

                    Horario6a12.IsChecked = _horariosDisponibles.ContainsKey("6:00 AM - 12:00 PM") && _horariosDisponibles["6:00 AM - 12:00 PM"];
                    Horario12a3.IsChecked = _horariosDisponibles.ContainsKey("12:00 PM - 03:00 PM") && _horariosDisponibles["12:00 PM - 03:00 PM"];
                    Horario3a5.IsChecked = _horariosDisponibles.ContainsKey("03:00 PM - 05:00 PM") && _horariosDisponibles["03:00 PM - 05:00 PM"];
                    Horario5a7.IsChecked = _horariosDisponibles.ContainsKey("05:00 PM - 07:00 PM") && _horariosDisponibles["05:00 PM - 07:00 PM"];
                    Horario7a8.IsChecked = _horariosDisponibles.ContainsKey("07:00 PM - 08:00 PM") && _horariosDisponibles["07:00 PM - 08:00 PM"];

                }
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"No se pudo cargar la disponibilidad: {ex.Message}", Colors.Red, Colors.White);
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }

        private void OnDateSelected(object sender, DateChangedEventArgs e)
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
                if (checkBox == Horario6a12) hora = "6:00 AM - 12:00 PM";
                else if (checkBox == Horario12a3) hora = "12:00 PM - 03:00 PM";
                else if (checkBox == Horario3a5) hora = "03:00 PM - 05:00 PM";
                else if (checkBox == Horario5a7) hora = "05:00 PM - 07:00 PM";
                else if (checkBox == Horario7a8) hora = "07:00 PM - 08:00 PM";


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
                var horaInicioTexto = hora.Split('-')[0].Trim();

                if (DateTime.TryParse(horaInicioTexto, out DateTime horaDateTime))
                {
                    var citasAfectadas = _citas.Where(c => c.Fecha.Hour == horaDateTime.Hour).ToList();

                    if (citasAfectadas.Any())
                    {
                        var popup = new CustomAlertPopup("Hay citas programadas para este horario. Si lo marca como no disponible, estas citas se cancelarán. ¿Desea continuar?\"");
                        bool confirmacion = await popup.ShowAsync(this);
                        if (!confirmacion)
                        {
                            _horariosDisponibles[hora] = true;
                            ActualizarCheckbox(hora, true);
                        }
                    }
                }
            }
        }


        private void ActualizarCheckbox(string hora, bool estado)
        {
            switch (hora)
            {
                case "6:00 AM - 12:00 PM": Horario6a12.IsChecked = estado; break;
                case "12:00 PM - 03:00 PM": Horario12a3.IsChecked = estado; break;
                case "03:00 PM - 05:00 PM": Horario3a5.IsChecked = estado; break;
                case "05:00 PM - 07:00 PM": Horario5a7.IsChecked = estado; break;
                case "07:00 PM - 08:00 PM": Horario7a8.IsChecked = estado; break;
            }
        }


        // En GestionarDisponibilidadPage.xaml.cs, modifica el método OnGuardarClicked
        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                long idBarbero = Convert.ToInt64(await SecureStorage.Default.GetAsync("user_cedula"));
                var HorariosJSON = new Dictionary<string, bool>
                {
                     { "6:00 AM - 12:00 PM", Horario6a12.IsChecked },
                     { "12:00 PM - 03:00 PM", Horario12a3.IsChecked },
                     { "03:00 PM - 05:00 PM", Horario3a5.IsChecked  },
                     { "05:00 PM - 07:00 PM", Horario5a7.IsChecked  },
                     { "07:00 PM - 08:00 PM", Horario7a8.IsChecked }
                };

                var disponibilidad = new DisponibilidadModel
                {
                    Id = 0,
                    Fecha = FechaSelector.Date,
                    BarberoId = idBarbero,
                    HorariosDict = HorariosJSON
                };


                // Guardar disponibilidad
                bool result = await _disponibilidadService.GuardarDisponibilidad(disponibilidad);

                if (result)
                {
                    await AppUtils.MostrarSnackbar("La disponibilidad ha sido guardada correctamente", Colors.Green, Colors.White);
                    await Navigation.PopAsync();
                }
                else
                {
                    await AppUtils.MostrarSnackbar("No se pudo guardar la disponibilidad", Colors.Red, Colors.White);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al guardar: {ex.Message}", "Aceptar");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
                _isNavigating = false;
            }
        }
        private async void OnConfigurarPlantillaClicked(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                await Navigation.PushAsync(new PlantillaDisponibilidadPage(_disponibilidadService));
            }
            finally
            {
                _isNavigating = false;
            }
            
        }

        private async void OnAplicarPlantillaMesClicked(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                var barberoId = AuthService.CurrentUser?.Cedula ?? 0;
                var plantilla = await _disponibilidadService.ObtenerPlantillaSemanal(barberoId);

                if (plantilla == null)
                {
                    await DisplayAlert("Aviso", "Primero debes configurar tu plantilla semanal", "OK");
                    return;
                }

                var popup = new CustomAlertPopup("¿Deseas aplicar tu plantilla semanal a todo el mes actual? Esto sobrescribirá la disponibilidad existente.");
                bool confirmacion = await popup.ShowAsync(this);

                if (!confirmacion)
                    return;

                // Aplicar al mes actual
                var primerDia = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                var ultimoDia = primerDia.AddMonths(1).AddDays(-1);

                bool resultado = await _disponibilidadService.AplicarPlantillaARango(barberoId, primerDia, ultimoDia);

                if (resultado)
                {
                    await AppUtils.MostrarSnackbar("Plantilla aplicada correctamente al mes", Colors.Green, Colors.White);
                    await LoadDisponibilidad(); // Recargar la vista actual
                }
                else
                {
                    await AppUtils.MostrarSnackbar("Error al aplicar la plantilla", Colors.Red, Colors.White);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
                _isNavigating = false;
            }
        }
    }
}