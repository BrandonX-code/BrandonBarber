namespace Gasolutions.Maui.App.Pages
{
    public partial class RegistroPage : ContentPage
    {
        private readonly AuthService _authService;
        private readonly BarberiaService _barberiaService;
        private Barberia _selectedBarberia;
        private List<Barberia> _allBarberias;
        public ObservableCollection<Barberia> Barberias { get; } = new ObservableCollection<Barberia>();

        public RegistroPage()
        {
            InitializeComponent();
            _authService = Application.Current.Handler.MauiContext.Services.GetService<AuthService>();
            _barberiaService = Application.Current.Handler.MauiContext.Services.GetService<BarberiaService>();

            // Cargar barberías
            LoadBarberias();
        }
        private async void LoadBarberias()
        {
            try
            {
                // Mostrar indicador de carga
                _allBarberias = await _barberiaService.GetBarberiasAsync();

                // Mostrar todas inicialmente
                UpdateBarberiaList(string.Empty);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error cargando barberías: {ex.Message}", "OK");
            }
        }

        private void UpdateBarberiaList(string searchText)
        {
            Barberias.Clear();

            var filtered = _allBarberias.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filtered = filtered.Where(b =>
                    (b.Nombre?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true ||
                    (b.Direccion?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true)));
            }

            foreach (var barberia in filtered)
            {
                Barberias.Add(barberia);
            }
        }
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateBarberiaList(e.NewTextValue);
        }
        private async void OnSelectBarberiaTapped(object sender, EventArgs e)
        {
            // Crea la instancia de la página de selección
            var selectionPage = new SeleccionBarberiaPage();

            // Suscribe el evento ANTES de navegar
            selectionPage.BarberiaSeleccionada += (s, barberia) =>
            {
                _selectedBarberia = barberia;
                SelectedBarberiaFrame.IsVisible = true;
                SelectedBarberiaName.Text = barberia.Nombre;
                SelectedBarberiaEmail.Text = barberia.Email;
                SelectedBarberiaTelefono.Text = barberia.Telefono;
                SelectedBarberiaAddress.Text = barberia.Direccion;
                SelectedBarberiaPlaceholder.IsVisible = false;
            };

            // Navega a la MISMA instancia a la que te suscribiste
            await Navigation.PushAsync(selectionPage); // Cambiado a PushAsync
        }
        private async void OnRegistrarClicked(object sender, EventArgs e)
        {
            // Validar que se haya seleccionado una barbería
            if (_selectedBarberia == null)
            {
                await DisplayAlert("Error", "Debe seleccionar una barbería", "OK");
                return;
            }
            if (!ValidarFormulario())
            {
                return;
            }

            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            ErrorLabel.IsVisible = false;

            var registroButton = (Button)sender;
            registroButton.IsEnabled = false;

            try
            {
                var registroRequest = new RegistroRequest
                {
                    Nombre = NombreEntry.Text,
                    Cedula = long.Parse(CedulaEntry.Text),
                    Email = EmailEntry.Text,
                    Contraseña = PasswordEntry.Text,
                    ConfirmContraseña = ConfirmPasswordEntry.Text,
                    Telefono = TelefonoEntry.Text,
                    Direccion = DireccionEntry.Text,
                    IdBarberia = _selectedBarberia.Idbarberia,
                    Rol = "cliente" // Siempre cliente en registro público
                };

                var response = await _authService.Register(registroRequest);
                Console.WriteLine($"Respuesta de registro: Success = {response.IsSuccess}, Message = {response.Message}");

                if (response.IsSuccess)
                {
                    await DisplayAlert("Registro Exitoso", response.Message, "OK");
                    await Navigation.PushAsync(new LoginPage());
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
                registroButton.IsEnabled = true;
            }
        }

        private bool ValidarFormulario()
        {
            if (string.IsNullOrWhiteSpace(NombreEntry.Text) ||
                string.IsNullOrWhiteSpace(CedulaEntry.Text) ||
                string.IsNullOrWhiteSpace(EmailEntry.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry.Text) ||
                string.IsNullOrWhiteSpace(ConfirmPasswordEntry.Text))
            {
                ErrorLabel.Text = "Por favor, completa todos los campos obligatorios";
                ErrorLabel.IsVisible = true;
                return false;
            }

            if (!long.TryParse(CedulaEntry.Text, out _))
            {
                ErrorLabel.Text = "La cédula debe ser un número válido";
                ErrorLabel.IsVisible = true;
                return false;
            }

            if (!IsValidEmail(EmailEntry.Text))
            {
                ErrorLabel.Text = "El formato del correo electrónico no es válido";
                ErrorLabel.IsVisible = true;
                return false;
            }

            if (PasswordEntry.Text != ConfirmPasswordEntry.Text)
            {
                ErrorLabel.Text = "Las contraseñas no coinciden";
                ErrorLabel.IsVisible = true;
                return false;
            }

            if (!IsPasswordSecure(PasswordEntry.Text))
            {
                ErrorLabel.Text = "La contraseña debe tener al menos 8 caracteres, incluir letras mayúsculas, minúsculas y números";
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