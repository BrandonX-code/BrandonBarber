using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gasolutions.Maui.App.Pages
{
    public partial class CalificarBarberoPage : ContentPage
    {
        private readonly UsuarioModels _barbero;
        private int _calificacionSeleccionada;
        private readonly List<ImageButton> _estrellas;

        public CalificarBarberoPage(UsuarioModels barbero)
        {
            InitializeComponent();
            _barbero = barbero;
            _estrellas = new List<ImageButton>
            {
                Estrella1, Estrella2, Estrella3, Estrella4, Estrella5
            };

            // Cargar la calificación previa al aparecer la página
            this.Appearing += async (s, e) => await CargarCalificacionPrevia();
        }

        private async Task CargarCalificacionPrevia()
        {
            var calificacionService = Application.Current.Handler.MauiContext.Services.GetService<CalificacionService>();
            var clienteId = AuthService.CurrentUser.Cedula;
            var puntuacion = await calificacionService.ObtenerCalificacionClienteAsync(_barbero.Cedula, clienteId);

            _calificacionSeleccionada = puntuacion;
            for (int i = 0; i < _estrellas.Count; i++)
            {
                _estrellas[i].Source = i < _calificacionSeleccionada ? "star_filled.png" : "star_empty.png";
            }
        }

        private void OnEstrellaClicked(object sender, EventArgs e)
        {
            var estrella = sender as ImageButton;
            var index = _estrellas.IndexOf(estrella) + 1;
            _calificacionSeleccionada = index;

            // Actualizar visualización de estrellas
            for (int i = 0; i < _estrellas.Count; i++)
            {
                _estrellas[i].Source = i < index ? "star_filled.png" : "star_empty.png";
            }
        }

        private async void OnEnviarCalificacionClicked(object sender, EventArgs e)
        {
            if (_calificacionSeleccionada == 0)
            {
                await DisplayAlert("Error", "Por favor selecciona una calificación", "OK");
                return;
            }

            var calificacion = new CalificacionModel
            {
                BarberoId = _barbero.Cedula,
                ClienteId = AuthService.CurrentUser.Cedula,
                Puntuacion = _calificacionSeleccionada,
                Comentario = ComentarioEditor.Text,
                FechaCalificacion = DateTime.Now
            };

            var calificacionService = Application.Current.Handler.MauiContext.Services.GetService<CalificacionService>();
            var exito = await calificacionService.EnviarCalificacionAsync(calificacion);

            if (exito)
            {
                await DisplayAlert("Éxito", "¡Gracias por tu calificación!", "OK");
                // Notifica a la página anterior que debe refrescar el promedio
                MessagingCenter.Send(this, "CalificacionEnviada", _barbero.Cedula);
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Error", "No se pudo enviar la calificación", "OK");
            }
        }
    }
}
