namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class EditarPerfilPage : ContentPage
    {
        private readonly UsuarioModels _perfilData;
        private readonly BarberiaService _barberiaService;

        private readonly PerfilUsuarioService _perfilService;

        private bool _imagenModificada = false;
        private bool _isNavigating = false;
        public EditarPerfilPage(UsuarioModels? perfilData = null)
        {
            InitializeComponent();

            _perfilService = Application.Current!.Handler.MauiContext!.Services.GetService<PerfilUsuarioService>()!;
            _barberiaService = Application.Current!.Handler.MauiContext!.Services.GetService<BarberiaService>()!; // NUEVO
            _perfilData = perfilData ?? new UsuarioModels
            {
                Nombre = "Carlos Álvarez",
                Telefono = "555-123-4567",
                Email = "",
                Direccion = "",
                ImagenPath = "default_avatar.png",
                Rol = "Cliente" // Valor por defecto
            };

            CargarDatosPerfil();
            _= ConfigurarVisibilidadPorRol();
        }

        private async Task ConfigurarVisibilidadPorRol()
        {
            bool esBarbero = _perfilData.Rol?.Equals("Barbero", StringComparison.OrdinalIgnoreCase) ?? false;
            bool esCliente = _perfilData.Rol?.Equals("Cliente", StringComparison.OrdinalIgnoreCase) ?? false;

            // Configurar especialidades (solo barberos)
            EspecialidadesContainer.IsVisible = esBarbero;

            // Configurar sección de barbería actual (solo clientes con barbería asignada)
            BarberiaActualContainer.IsVisible = esCliente && _perfilData.IdBarberia.HasValue;

            // Configurar botón de cambiar barbería (solo clientes)
            CambiarBarberiaContainer.IsVisible = esCliente;

            // Cargar información de la barbería si es cliente y tiene una asignada
            if (esCliente && _perfilData.IdBarberia.HasValue)
            {
                await CargarInformacionBarberia(_perfilData.IdBarberia.Value);
            }
        }
        private async Task CargarInformacionBarberia(int idBarberia)
        {
            try
            {
                var barberia = await _barberiaService.GetBarberiaByIdAsync(idBarberia);

                if (barberia != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        BarberiaNombreLabel.Text = barberia.Nombre ?? "Sin nombre";
                        BarberiaDireccionLabel.Text = barberia.Direccion ?? "Sin dirección";
                        BarberiaTelefonoLabel.Text = barberia.Telefono ?? "Sin teléfono";

                        // Cargar logo
                        if (!string.IsNullOrWhiteSpace(barberia.LogoUrl))
                        {
                            BarberiaLogoImage.Source = barberia.LogoUrl.StartsWith("http")
                                ? ImageSource.FromUri(new Uri(barberia.LogoUrl))
                                : ImageSource.FromFile(barberia.LogoUrl);
                        }
                        else
                        {
                            BarberiaLogoImage.Source = "picture.png";
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al cargar barbería: {ex.Message}");
                // Mostrar valores por defecto
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    BarberiaNombreLabel.Text = "No disponible";
                    BarberiaDireccionLabel.Text = "Sin información";
                    BarberiaTelefonoLabel.Text = "Sin teléfono";
                });
            }
        }

        private async void CargarDatosPerfil()
        {
            NombreEntry.Text = _perfilData.Nombre;
            TelefonoEntry.Text = _perfilData.Telefono;
            EmailEntry.Text = _perfilData.Email;
            DireccionEntry.Text = _perfilData.Direccion;
            EspecialidadesEntry.Text = _perfilData.Especialidades;

            if (!string.IsNullOrEmpty(_perfilData.ImagenPath) && _perfilData.ImagenPath != "default_avatar.png")
            {
                try
                {
                    PerfilImage.Source = _perfilData.ImagenPath.StartsWith("http")
                        ? ImageSource.FromUri(new Uri(_perfilData.ImagenPath))
                        : ImageSource.FromFile(_perfilData.ImagenPath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error al cargar la imagen: {ex.Message}");
                    PerfilImage.Source = "default_avatar.png";
                }
            }

            // Configurar visibilidad basada en el rol (esto cargará la barbería si aplica)
            await ConfigurarVisibilidadPorRol();
        }

        private async void OnCambiarImagenClicked(object sender, EventArgs e)
        {
            try
            {
                var options = new PickOptions
                {
                    PickerTitle = "Seleccione una imagen de perfil",
                    FileTypes = FilePickerFileType.Images
                };

                var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions { });
                if (result != null)
                {
                    PerfilImage.Source = result.FullPath;
                    _perfilData.ImagenPath = result.FullPath;
                    _imagenModificada = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo seleccionar la imagen: {ex.Message}", "OK");
            }
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            IsBusy = true;

            try
            {
                _perfilData.Nombre = NombreEntry.Text;
                _perfilData.Telefono = TelefonoEntry.Text;
                _perfilData.Email = EmailEntry.Text;
                _perfilData.Direccion = DireccionEntry.Text;

                // Guardar especialidades solo si es barbero
                if (_perfilData.Rol?.Equals("Barbero", StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    _perfilData.Especialidades = EspecialidadesEntry.Text;
                }

                bool perfilGuardado = await _perfilService.SavePerfilUsuario(_perfilData);
                bool imagenActualizada = true;

                if (perfilGuardado && _imagenModificada && !_perfilData.ImagenPath!.StartsWith("http"))
                {
                    imagenActualizada = await _perfilService.UpdateProfileImage(_perfilData.Cedula, _perfilData.ImagenPath);

                    if (imagenActualizada)
                    {
                        var updatedPerfil = await _perfilService.GetPerfilUsuario(_perfilData.Cedula);
                        if (updatedPerfil != null)
                        {
                            _perfilData.ImagenPath = updatedPerfil.ImagenPath;
                        }
                    }
                }

                if (perfilGuardado)
                {
                    await AppUtils.MostrarSnackbar("Los cambios se han guardado correctamente", Colors.Green, Colors.White);
                    await Navigation.PopAsync();
                }

                else
                {
                    await AppUtils.MostrarSnackbar("No se pudieron guardar todos los cambios.", Colors.Red, Colors.White);
                }
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error al guardar los cambios: {ex.Message}", Colors.Red, Colors.White);
            }
            finally
            {
                IsBusy = false;
            }
        }
        private async void OnCambiarDeBarberia(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;

            try
            {
                var seleccionPage = new SeleccionBarberiaPage();

                seleccionPage.BarberiaSeleccionada += async (s, barberia) =>
                {
                    var authService = Application.Current!.Handler.MauiContext!.Services.GetService<AuthService>()!;

                    //bool confirmado = await DisplayAlert(
                    //    "Confirmar cambio",
                    //    $"¿Deseas cambiar a la barbería '{barberia.Nombre}'?",
                    //    "Sí", "No");
                    var popup = new CustomAlertPopup($"¿Deseas cambiar a la barbería '{barberia.Nombre}'?");
                    bool confirmacion = await popup.ShowAsync(this);

                    if (confirmacion)
                    {
                        bool exito = await authService.CambiarBarberia(_perfilData.Cedula, barberia.Idbarberia);

                        if (exito)
                        {
                            _perfilData.IdBarberia = barberia.Idbarberia;

                            // Actualizar la UI con la nueva barbería
                            await CargarInformacionBarberia(barberia.Idbarberia);

                            await AppUtils.MostrarSnackbar($"Cambiado a {barberia.Nombre}", Colors.Green, Colors.White);
                        }
                        else
                        {
                            await AppUtils.MostrarSnackbar("Error al cambiar de barbería", Colors.Red, Colors.White);
                        }
                    }
                };

                await Navigation.PushAsync(seleccionPage);
            }
            finally
            {
                _isNavigating = false;
            }
        }

    }
}