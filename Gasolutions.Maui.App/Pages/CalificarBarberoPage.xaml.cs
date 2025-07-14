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
            estrella.ScaleTo(1.2, 100, Easing.CubicOut);
            estrella.ScaleTo(1.0, 100, Easing.CubicIn);
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
                await AppUtils.MostrarSnackbar("Por favor selecciona una calificación", Colors.Orange, Colors.White);
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
                await AppUtils.MostrarSnackbar("¡Gracias por tu calificación!", Colors.Green, Colors.White);
                // Notifica a la página anterior que debe refrescar el promedio
                MessagingCenter.Send(this, "CalificacionEnviada", _barbero.Cedula);
                await Navigation.PopAsync();
            }
            else
            {
                await AppUtils.MostrarSnackbar("No se pudo enviar la calificación", Colors.Red, Colors.White);
            }
        }
    }
}
