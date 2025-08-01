using Gasolutions.Maui.App.Models;
using Gasolutions.Maui.App.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Gasolutions.Maui.App.Pages
{
    public partial class FormBarberiaPage : ContentPage, INotifyPropertyChanged
    {
        private readonly BarberiaService _barberiaService;
        private readonly bool _isEdit;
        private Barberia _barberiaOriginal;
        private byte[] _logoBytes;
        private string _logoFileName;

        public event EventHandler BarberiaGuardada;

        public string Nombre { get => _nombre; set { _nombre = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanSave)); } }
        public string Telefono { get => _telefono; set { _telefono = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanSave)); } }
        public string Direccion { get => _direccion; set { _direccion = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanSave)); } }
        public string Email { get => _email; set { _email = value; OnPropertyChanged(); } }
        public string LogoUrl { get => _logoUrl; set { _logoUrl = value; OnPropertyChanged(); } }
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanSave)); } }

        public string PageTitle => _isEdit ? "Editar Barbería" : "Nueva Barbería";
        public string SaveButtonText => _isEdit ? "Actualizar" : "Crear Barbería";
        public bool CanSave => !IsBusy;

        private string _nombre = string.Empty;
        private string _telefono = string.Empty;
        private string _direccion = string.Empty;
        private string _email = string.Empty;
        private string _logoUrl = string.Empty;
        private bool _isBusy;
        private readonly long _idAdministrador;

        public FormBarberiaPage()
        {
            InitializeComponent();
            _isEdit = false;
            _barberiaService = Application.Current.Handler.MauiContext.Services.GetService<BarberiaService>();
            _idAdministrador = AuthService.CurrentUser.Cedula;
            BindingContext = this;
        }

        public FormBarberiaPage(Barberia barberia) : this()
        {
            _isEdit = true;
            _barberiaOriginal = barberia;
            Nombre = barberia.Nombre ?? string.Empty;
            Telefono = barberia.Telefono ?? string.Empty;
            Direccion = barberia.Direccion ?? string.Empty;
            Email = barberia.Email ?? string.Empty;
            LogoUrl = barberia.LogoUrl ?? string.Empty;
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            if (IsBusy) return;

            // Validación mejorada con mensajes específicos
            if (!ValidarCampos())
                return;

            IsBusy = true;

            try
            {
                var barberia = new Barberia
                {
                    Idbarberia = _isEdit ? _barberiaOriginal.Idbarberia : 0,
                    Idadministrador = _idAdministrador,
                    Nombre = Nombre.Trim(),
                    Telefono = Telefono.Trim(),
                    Direccion = Direccion.Trim(),
                    Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                    LogoUrl = _isEdit ? _barberiaOriginal.LogoUrl : null
                };

                bool success = _isEdit
                    ? await _barberiaService.UpdateBarberiaAsync(barberia)
                    : await _barberiaService.CreateBarberiaAsync(barberia);

                if (success && _logoBytes != null && _logoFileName != null)
                {
                    if (!_isEdit)
                    {
                        var barberias = await _barberiaService.GetBarberiasByAdministradorAsync(_idAdministrador);
                        var nuevaBarberia = barberias.OrderByDescending(b => b.Idbarberia).FirstOrDefault();
                        if (nuevaBarberia != null) barberia.Idbarberia = nuevaBarberia.Idbarberia;
                    }

                    bool logoSuccess = await _barberiaService.UploadBarberiaLogoAsync(barberia.Idbarberia, _logoBytes, _logoFileName);
                    if (!logoSuccess)
                    {
                        await AppUtils.MostrarSnackbar("La barbería se guardó, pero hubo error al subir el logo", Colors.Orange, Colors.White);
                    }
                }

                if (success)
                {
                    await AppUtils.MostrarSnackbar(_isEdit ? "Barbería actualizada" : "Barbería creada", Colors.Green, Colors.White);
                    BarberiaGuardada?.Invoke(this, EventArgs.Empty);
                    await Navigation.PopAsync();
                }
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error al guardar barbería: {ex.Message}", Colors.Red, Colors.White);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool ValidarCampos()
        {
            var camposVacios = new List<string>();

            if (string.IsNullOrWhiteSpace(Nombre))
                camposVacios.Add("Nombre");

            if (string.IsNullOrWhiteSpace(Telefono))
                camposVacios.Add("Teléfono");

            if (string.IsNullOrWhiteSpace(Direccion))
                camposVacios.Add("Dirección");

            if (camposVacios.Any())
            {
                ErrorLabel.Text = "Por favor, completa todos los campos obligatorios (*)";
                ErrorLabel.IsVisible = true;
                return false;
            }

            // Validación adicional para email si no está vacío
            if (!string.IsNullOrWhiteSpace(Email) && !IsValidEmail(Email))
            {
                ErrorLabel.Text = "Por favor ingresa un email válido";
                ErrorLabel.IsVisible = true;
                return false;
            }

            // Si todo está correcto, ocultar el error
            ErrorLabel.IsVisible = false;
            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void OnSelectLogoTapped(object sender, TappedEventArgs e)
        {
            OnSelectLogoClicked(sender, EventArgs.Empty);
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnSelectLogoClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions { Title = "Seleccionar logo de la barbería" });
                if (result == null) return;

                using var stream = await result.OpenReadAsync();
                if (stream.Length > 5 * 1024 * 1024)
                {
                    await AppUtils.MostrarSnackbar("La imagen no puede superar los 5MB", Colors.Red, Colors.White);
                    return;
                }

                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                _logoBytes = memoryStream.ToArray();
                _logoFileName = result.FileName;

                var tempPath = Path.Combine(FileSystem.CacheDirectory, result.FileName);
                await File.WriteAllBytesAsync(tempPath, _logoBytes);
                LogoUrl = tempPath;

                await AppUtils.MostrarSnackbar("Logo seleccionado correctamente", Colors.Green, Colors.White);
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error al seleccionar imagen: {ex.Message}", Colors.Red, Colors.White);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}