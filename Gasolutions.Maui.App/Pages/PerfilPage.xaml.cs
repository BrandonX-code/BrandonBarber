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

        private long _currentUserCedula = 123456789;

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

                var perfil = await _perfilService.GetPerfilUsuario(_currentUserCedula);

                if (perfil != null)
                {
                    ActualizarPerfil(perfil);
                }
                else
                {
                    _perfilData = new PerfilUsuario
                    {
                        Cedula = _currentUserCedula,
                        Nombre = "xxx",
                        Telefono = "###",
                        Email = "",
                        Direccion = "",
                        ImagenPath = "default_avatar.png"
                    };

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
    }
}