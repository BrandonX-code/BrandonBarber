namespace Gasolutions.Maui.App.Pages
{
    public partial class EditarPerfilPage : ContentPage
    {
        private readonly UsuarioModels _perfilData;

        private readonly PerfilUsuarioService _perfilService;

        private bool _imagenModificada = false;

        public EditarPerfilPage(UsuarioModels? perfilData = null)
        {
            InitializeComponent();

            _perfilService = Application.Current!.Handler.MauiContext!.Services.GetService<PerfilUsuarioService>()!;

            _perfilData = perfilData ?? new UsuarioModels
            {
                Nombre = "Carlos Álvarez",
                Telefono = "555-123-4567",
                Email = "",
                Direccion = "",
                ImagenPath = "default_avatar.png"
            };

            CargarDatosPerfil();
        }

        private void CargarDatosPerfil()
        {
            NombreEntry.Text = _perfilData.Nombre;
            TelefonoEntry.Text = _perfilData.Telefono;
            EmailEntry.Text = _perfilData.Email;
            DireccionEntry.Text = _perfilData.Direccion;

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

                var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions {});
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
                    await AppUtils.MostrarSnackbar( "Los cambios se han guardado correctamente", Colors.Green, Colors.White);
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


    }
}