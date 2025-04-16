using System;
using Microsoft.Maui.Controls;
using Gasolutions.Maui.App.Models;
using Gasolutions.Maui.App.Services;
using System.Threading.Tasks;

namespace Gasolutions.Maui.App.Pages
{
    public partial class PerfilPage : ContentPage
    {
        private PerfilUsuario _perfilData;
        private readonly PerfilUsuarioService _perfilService;

        public PerfilPage()
        {
            InitializeComponent();
            _perfilService = Application.Current.Handler.MauiContext.Services.GetService<PerfilUsuarioService>();

            MessagingCenter.Subscribe<EditarPerfilPage, PerfilUsuario>(
                this, "PerfilActualizado", (sender, perfilActualizado) =>
                {
                    ActualizarPerfil(perfilActualizado);
                });

            this.Appearing += async (sender, e) => await CargarDatosPerfil();
        }

        private async Task CargarDatosPerfil()
        {
            try
            {
                IsBusy = true;

                // Usar el usuario actual de AuthService
                if (AuthService.CurrentUser == null)
                {
                    await DisplayAlert("Error", "No hay usuario conectado", "OK");
                    return;
                }

                // Obtener perfil usando la cédula del usuario actual
                var perfil = await _perfilService.GetPerfilUsuario(AuthService.CurrentUser.Cedula);

                if (perfil != null)
                {
                    ActualizarPerfil(perfil);
                }
                else
                {
                    // Si no existe, crear uno con los datos del login
                    _perfilData = new PerfilUsuario
                    {
                        Cedula = AuthService.CurrentUser.Cedula,
                        Nombre = AuthService.CurrentUser.Nombre,
                        Email = AuthService.CurrentUser.Email,
                        Telefono = AuthService.CurrentUser.Telefono ?? "Sin teléfono",
                        Direccion = "", // Este dato no lo tenemos del login
                        ImagenPath = "default_avatar.png"
                    };

                    // Guardarlo en la base de datos para futuras ediciones
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

        private void ActualizarPerfil(PerfilUsuario perfilActualizado)
        {
            _perfilData = perfilActualizado;
            ActualizarUI();
        }

        private void ActualizarUI()
        {
            NombreLabel.Text = _perfilData.Nombre;
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
            var confirmacion = await DisplayAlert("Cerrar sesión", "¿Estás seguro que deseas cerrar sesión?", "Sí", "Cancelar");

            if (confirmacion)
            {
                // Elimina los datos guardados
                Preferences.Remove("isLoggedIn");
                Preferences.Remove("currentUser");

                // Redirige al login
                Application.Current.MainPage = new NavigationPage(new LoginPage());
            }
        }

    }
}