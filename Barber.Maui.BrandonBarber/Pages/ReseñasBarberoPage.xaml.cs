using Barber.Maui.BrandonBarber.Services;

namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class ReseñasBarberoPage : ContentPage
    {
        private readonly UsuarioModels _barbero;

        public ReseñasBarberoPage(UsuarioModels barbero)
        {
            InitializeComponent();
            _barbero = barbero;

            this.Appearing += async (s, e) => await CargarResenas();
        }

        private async Task CargarResenas()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                var calificacionService = Application.Current?.Handler?.MauiContext?.Services?
                    .GetService<CalificacionService>();

                var authService = Application.Current?.Handler?.MauiContext?.Services?
                    .GetService<AuthService>();

                if (calificacionService == null || authService == null)
                {
                    await DisplayAlert("Error", "Servicios no encontrados", "OK");
                    return;
                }

                var reseñas = await calificacionService.ObtenerResenasAsync(_barbero.Cedula);

                var items = new List<object>();

                foreach (var r in reseñas)
                {
                    var cliente = await authService.GetUserByCedula(r.ClienteId);

                    string? nombreCliente = cliente != null
                        ? cliente.Nombre
                        : "Cliente desconocido";

                    items.Add(new
                    {
                        NombreCliente = nombreCliente,
                        Estrellas = new string('★', r.Puntuacion) + new string('☆', 5 - r.Puntuacion),
                        Puntuacion = $"{r.Puntuacion}/5",
                        Comentario = string.IsNullOrEmpty(r.Comentario) ? "" : r.Comentario,
                        Fecha = r.FechaCalificacion.ToString("dd/MM/yyyy")
                    });
                }

                ResenasCollection.ItemsSource = items;

                EmptyStateLayout.IsVisible = items.Count == 0;
                ResenasCollection.IsVisible = items.Count > 0;

            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }

    }
}
