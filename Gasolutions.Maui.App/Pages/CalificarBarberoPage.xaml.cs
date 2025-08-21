using CommunityToolkit.Mvvm.Messaging;
using Gasolutions.Maui.App.Messenger;

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
            try
            {
                // Validar que el usuario actual no sea null
                if (AuthService.CurrentUser == null)
                {
                    await AppUtils.MostrarSnackbar("Error: Usuario no autenticado", Colors.Red, Colors.White);
                    return;
                }

                var calificacionService = Application.Current?.Handler?.MauiContext?.Services?.GetService<CalificacionService>();
                if (calificacionService == null)
                {
                    await AppUtils.MostrarSnackbar("Error: Servicio no disponible", Colors.Red, Colors.White);
                    return;
                }

                var clienteId = AuthService.CurrentUser.Cedula;
                var puntuacion = await calificacionService.ObtenerCalificacionClienteAsync(_barbero.Cedula, clienteId);
                _calificacionSeleccionada = puntuacion;

                // CORRECCIÓN: Remover los asteriscos (*) incorrectos
                for (int i = 0; i < _estrellas.Count; i++)
                {
                    _estrellas[i].Source = i < _calificacionSeleccionada ? "star_filled.png" : "star_empty.png";
                }
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error al cargar calificación previa: {ex.Message}", Colors.Red, Colors.White);
            }
        }

        private async void OnEstrellaClicked(object sender, EventArgs e)
        {
            try
            {
                var estrella = sender as ImageButton;
                var index = _estrellas.IndexOf(estrella) + 1;
                _calificacionSeleccionada = index;

                // Animación
                await estrella.ScaleTo(1.2, 100, Easing.CubicOut);
                await estrella.ScaleTo(1.0, 100, Easing.CubicIn);

                // CORRECCIÓN: Actualizar visualización de estrellas correctamente
                for (int i = 0; i < _estrellas.Count; i++)
                {
                    _estrellas[i].Source = i < index ? "star_filled.png" : "star_empty.png";
                }
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error al seleccionar estrella: {ex.Message}", Colors.Red, Colors.White);
            }
        }

        private async void OnEnviarCalificacionClicked(object sender, EventArgs e)
        {
            try
            {
                if (_calificacionSeleccionada == 0)
                {
                    await AppUtils.MostrarSnackbar("Por favor selecciona una calificación", Colors.Orange, Colors.White);
                    return;
                }

                // Validar usuario actual
                if (AuthService.CurrentUser == null)
                {
                    await AppUtils.MostrarSnackbar("Error: Usuario no autenticado", Colors.Red, Colors.White);
                    return;
                }

                var calificacionService = Application.Current?.Handler?.MauiContext?.Services?.GetService<CalificacionService>();
                if (calificacionService == null)
                {
                    await AppUtils.MostrarSnackbar("Error: Servicio no disponible", Colors.Red, Colors.White);
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

                var exito = await calificacionService.EnviarCalificacionAsync(calificacion);

                if (exito)
                {
                    await AppUtils.MostrarSnackbar("¡Gracias por tu calificación!", Colors.Green, Colors.White);
                    WeakReferenceMessenger.Default.Send(new CalificacionEnviadaMessage(_barbero.Cedula));
                    await Navigation.PopAsync();
                }
                else
                {
                    await AppUtils.MostrarSnackbar("No se pudo enviar la calificación", Colors.Red, Colors.White);
                }
            }
            catch (Exception ex)
            {
                await AppUtils.MostrarSnackbar($"Error al enviar calificación: {ex.Message}", Colors.Red, Colors.White);
            }
        }
    }
}