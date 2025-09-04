using CommunityToolkit.Mvvm.Messaging.Messages;
using Barber.Maui.BrandonBarber.Mobal;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class PerfilPage : ContentPage
    {
        private UsuarioModels? _perfilData;
        private readonly PerfilUsuarioService? _perfilService;

        public PerfilPage()
        {
            InitializeComponent();
            _perfilService = Application.Current!.Handler.MauiContext!.Services.GetService<PerfilUsuarioService>();

            WeakReferenceMessenger.Default.Register<PerfilActualizadoMessage>(this, (r, m) =>
            {
                ActualizarPerfil(m.Value);
            });


            this.Appearing += async (sender, e) => await CargarDatosPerfil();
        }

        private async Task CargarDatosPerfil()
        {
            try
            {
                IsBusy = true;

                if (AuthService.CurrentUser == null)
                {
                    await DisplayAlert("Error", "No hay usuario conectado", "OK");
                    return;
                }

                var perfil = await _perfilService!.GetPerfilUsuario(AuthService.CurrentUser.Cedula);

                if (perfil != null)
                {
                    ActualizarPerfil(perfil);
                }
                else
                {
                    _perfilData = new UsuarioModels
                    {
                        Cedula = AuthService.CurrentUser.Cedula,
                        Nombre = AuthService.CurrentUser.Nombre,
                        Email = AuthService.CurrentUser.Email,
                        Telefono = AuthService.CurrentUser.Telefono ?? "Sin teléfono",
                        Direccion = "",
                        ImagenPath = "default_avatar.png"
                    };

                    await _perfilService.SavePerfilUsuario(_perfilData);

                    ActualizarUI();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al cargar el perfil: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ActualizarPerfil(UsuarioModels perfilActualizado)
        {
            _perfilData = perfilActualizado;
            ActualizarUI();
        }

        private void ActualizarUI()
        {
            NombreLabel.Text = _perfilData!.Nombre;
            TelefonoLabel.Text = _perfilData.Telefono;

            if (!string.IsNullOrEmpty(_perfilData.ImagenPath))
            {
                try
                {
                    PerfilImage.Source = _perfilData.ImagenPath.StartsWith("http")
                        ? ImageSource.FromUri(new Uri(_perfilData.ImagenPath))
                        : ImageSource.FromFile(_perfilData.ImagenPath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al cargar la imagen: {ex.Message}");
                    PerfilImage.Source = "default_avatar.png";
                }
            }
        }

        private async void OnEditarPerfilClicked(object sender, EventArgs e)
        {
            var editarPerfilPage = new EditarPerfilPage(_perfilData);
            await Navigation.PushAsync(editarPerfilPage);
        }
        private async void OnCerrarSesionClicked(object sender, EventArgs e)
        {
            var popup = new CustomAlertPopup("¿Quieres Cerrar Sesión?");
            bool confirmacion = await popup.ShowAsync(this);

            if (confirmacion)
            {
                Preferences.Remove("isLoggedIn");
                Preferences.Remove("currentUser");

                Application.Current!.Windows[0].Page = new NavigationPage(new LoginPage());

            }
        }

    }
}
public class PerfilActualizadoMessage : ValueChangedMessage<UsuarioModels>
{
    public PerfilActualizadoMessage(UsuarioModels value) : base(value) { }
}
