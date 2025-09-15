namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class AgregarBarberoPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly BarberiaService _barberiaService;
        private List<Barberia>? _barberias;
        private int _barberiaSeleccionadaId;
        public AgregarBarberoPage()
        {
            InitializeComponent();
            _authService = Application.Current!.Handler.MauiContext!.Services.GetService<AuthService>()!;
            _barberiaService = Application.Current!.Handler.MauiContext!.Services.GetService<BarberiaService>()!;

            CargarBarberias();
        }
        private async void CargarBarberias()
        {
            try
            {
                long idAdministrador = AuthService.CurrentUser!.Cedula;
                _barberias = await _barberiaService.GetBarberiasByAdministradorAsync(idAdministrador);

                BarberiaPicker.ItemsSource = _barberias;
                PickerSection.IsVisible = _barberias.Any();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudieron cargar las barberías: " + ex.Message, "OK");
            }
        }

        private void BarberiaPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            var picker = (Picker)sender;
            int selectedIndex = picker.SelectedIndex;
            if (selectedIndex != -1)
            {
                var barberiaSeleccionada = (Barberia)picker.SelectedItem;
                _barberiaSeleccionadaId = barberiaSeleccionada.Idbarberia;
            }
        }

        private async void OnAgregarBarberoClicked(object sender, EventArgs e)
        {
            if (!ValidarFormulario())
            {
                return;
            }

            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            ErrorLabel.IsVisible = false;
            SuccessLabel.IsVisible = false;

            var agregarButton = (Button)sender;
            agregarButton.IsEnabled = false;

            try
            {
                var registroRequest = new RegistroRequest
                {
                    Nombre = NombreEntry.Text,
                    Cedula = long.Parse(CedulaEntry.Text),
                    Email = EmailEntry.Text,
                    Contraseña = PasswordEntry.Text,
                    ConfirmContraseña = ConfirmPasswordEntry.Text,
                    Telefono = TelefonoEntry.Text ?? "",
                    Direccion = DireccionEntry.Text ?? "",
                    Especialidades = EspecialidadesEntry.Text ?? "",
                    Rol = "barbero", // Siempre barbero
                    IdBarberia = _barberiaSeleccionadaId
                };

                var response = await _authService.Register(registroRequest);

                if (response.IsSuccess)
                {
                    LimpiarFormulario();

                    await AppUtils.MostrarSnackbar("El barbero ha sido agregado correctamente", Colors.Green, Colors.White);
                }
                else
                {
                    ErrorLabel.Text = response.Message;
                    ErrorLabel.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = $"Error: {ex.Message}";
                ErrorLabel.IsVisible = true;
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
                agregarButton.IsEnabled = true;
            }
        }

        private void LimpiarFormulario()
        {
            NombreEntry.Text = "";
            CedulaEntry.Text = "";
            EmailEntry.Text = "";
            TelefonoEntry.Text = "";
            DireccionEntry.Text = "";
            EspecialidadesEntry.Text = "";
            ExperienciaEntry.Text = "";
            PasswordEntry.Text = "";
            ConfirmPasswordEntry.Text = "";
            // Agregar al final del método, antes del cierre
            _barberiaSeleccionadaId = 0;
            if (BarberiaPicker != null && _barberias?.Any() == true)
            {
                BarberiaPicker.SelectedIndex = -1;
            }
            //ActivoSwitch.IsToggled = true;
        }

        private bool ValidarFormulario()
        {
            // Limpiar mensajes anteriores
            ErrorLabel.IsVisible = false;
            SuccessLabel.IsVisible = false;

            // Validar campos obligatorios
            if (string.IsNullOrWhiteSpace(NombreEntry.Text) ||
                string.IsNullOrWhiteSpace(CedulaEntry.Text) ||
                string.IsNullOrWhiteSpace(EmailEntry.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry.Text) ||
                string.IsNullOrWhiteSpace(ConfirmPasswordEntry.Text))
            {
                ErrorLabel.Text = "Por favor, completa todos los campos obligatorios (*)";
                ErrorLabel.IsVisible = true;
                return false;
            }

            // Validar cédula numérica
            if (!long.TryParse(CedulaEntry.Text, out _))
            {
                ErrorLabel.Text = "La cédula debe ser un número válido";
                ErrorLabel.IsVisible = true;
                return false;
            }

            // Validar email
            if (!IsValidEmail(EmailEntry.Text))
            {
                ErrorLabel.Text = "El formato del correo electrónico no es válido";
                ErrorLabel.IsVisible = true;
                return false;
            }

            // Validar coincidencia de contraseñas
            if (PasswordEntry.Text != ConfirmPasswordEntry.Text)
            {
                ErrorLabel.Text = "Las contraseñas no coinciden";
                ErrorLabel.IsVisible = true;
                return false;
            }

            // Validar seguridad de contraseña
            if (!IsPasswordSecure(PasswordEntry.Text))
            {
                ErrorLabel.Text = "La contraseña debe tener al menos 8 caracteres, incluir letras mayúsculas, minúsculas y números";
                ErrorLabel.IsVisible = true;
                return false;
            }

            // Validar experiencia si se proporciona
            if (!string.IsNullOrWhiteSpace(ExperienciaEntry.Text) &&
                !int.TryParse(ExperienciaEntry.Text, out int experiencia))
            {
                ErrorLabel.Text = "Los años de experiencia deben ser un número válido";
                ErrorLabel.IsVisible = true;
                return false;
            }
            // Validar barbería seleccionada (agregar después de las validaciones existentes y antes del return true)
            if (_barberiaSeleccionadaId == 0)
            {
                ErrorLabel.Text = "Debe seleccionar una barbería";
                ErrorLabel.IsVisible = true;
                return false;
            }
            return true;
        }

        private bool IsValidEmail(string email)
        {
            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return regex.IsMatch(email);
        }

        private bool IsPasswordSecure(string password)
        {
            if (password.Length < 8)
                return false;

            if (!password.Any(char.IsUpper))
                return false;

            if (!password.Any(char.IsLower))
                return false;

            if (!password.Any(char.IsDigit))
                return false;

            return true;
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}