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
            if (!await ValidarFormulario())
                return;

            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsLoading = true;

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
                    Rol = "barbero",
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
                    await AppUtils.MostrarSnackbar(response.Message!, Colors.Red, Colors.White);
                }
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error: {ex.Message}", Colors.Red, Colors.White);
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
                agregarButton.IsEnabled = true;
            }
        }

        private void LimpiarFormulario()
        {
            NombreEntry.Text = string.Empty;
            CedulaEntry.Text = string.Empty;
            EmailEntry.Text = string.Empty;
            TelefonoEntry.Text = string.Empty;
            DireccionEntry.Text = string.Empty;
            EspecialidadesEntry.Text = string.Empty;
            ExperienciaEntry.Text = string.Empty;
            PasswordEntry.Text = string.Empty;
            ConfirmPasswordEntry.Text = string.Empty;
            // Agregar al final del método, antes del cierre
            _barberiaSeleccionadaId = 0;
            if (BarberiaPicker != null && _barberias?.Any() == true)
            {
                BarberiaPicker.SelectedIndex = -1;
            }
            //ActivoSwitch.IsToggled = true;
        }

        private async Task<bool> ValidarFormulario()
        {
            // Validar campos obligatorios
            if (string.IsNullOrWhiteSpace(NombreEntry.Text) ||
                string.IsNullOrWhiteSpace(CedulaEntry.Text) ||
                string.IsNullOrWhiteSpace(EmailEntry.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry.Text) ||
                string.IsNullOrWhiteSpace(ConfirmPasswordEntry.Text))
            {
                await AppUtils.MostrarSnackbar("Por favor, completa todos los campos obligatorios (*)", Colors.Red, Colors.White);
                return false;
            }

            // Validar cédula numérica
            if (!long.TryParse(CedulaEntry.Text, out _))
            {
                await AppUtils.MostrarSnackbar("La cédula debe ser un número válido", Colors.Red, Colors.White);
                return false;
            }

            // Validar email
            if (!IsValidEmail(EmailEntry.Text))
            {
                await AppUtils.MostrarSnackbar("El formato del correo electrónico no es válido", Colors.Red, Colors.White);
                return false;
            }

            // Validar coincidencia de contraseñas
            if (PasswordEntry.Text != ConfirmPasswordEntry.Text)
            {
                await AppUtils.MostrarSnackbar("Las contraseñas no coinciden", Colors.Red, Colors.White);
                return false;
            }

            // Validar seguridad de contraseña
            if (!IsPasswordSecure(PasswordEntry.Text))
            {
                await AppUtils.MostrarSnackbar("La contraseña debe tener al menos 8 caracteres, incluir letras mayúsculas, minúsculas y números", Colors.Red, Colors.White);
                return false;
            }

            // Validar experiencia si existe
            if (!string.IsNullOrWhiteSpace(ExperienciaEntry.Text) &&
                !int.TryParse(ExperienciaEntry.Text, out _))
            {
                await AppUtils.MostrarSnackbar("Los años de experiencia deben ser un número válido", Colors.Red, Colors.White);
                return false;
            }

            // Validar barbería seleccionada
            if (_barberiaSeleccionadaId == 0)
            {
                await AppUtils.MostrarSnackbar("Debe seleccionar una barbería", Colors.Red, Colors.White);
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