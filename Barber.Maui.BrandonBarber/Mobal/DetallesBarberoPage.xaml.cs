using System.Text.Json.Serialization;

namespace Barber.Maui.BrandonBarber.Mobal
{
    public partial class DetallesBarberoPage : ContentPage
    {
        public DetallesBarberoPage(UsuarioModels barbero)
        {
            InitializeComponent();
            BindingContext = new DetallesBarberoViewModel(barbero);
        }

        private async void OnCerrarClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }

    public class DetallesBarberoViewModel
    {
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Cedula { get; set; }
        public string Especialidades { get; set; }
        public string Rol { get; set; }
        public string InicialNombre { get; set; }
        public string? ImagenPath { get; set; }
        public bool TieneImagen { get; set; }
        public bool NoTieneImagen { get; set; }
        public string Telefono { get; set; }
        public string Direccion { get; set; }
        public string CalificacionTexto { get; set; }

        public DetallesBarberoViewModel(UsuarioModels barbero)
        {
            Nombre = barbero.Nombre ?? "No disponible";
            Email = barbero.Email ?? "No disponible";
            Cedula = barbero.Cedula.ToString() ?? "No disponible";
            Especialidades = barbero.Especialidades ?? "No disponible";
            Rol = barbero.Rol ?? "No disponible";
            Telefono = barbero.Telefono ?? "No disponible";
            Direccion = barbero.Direccion ?? "No disponible";

            // Obtener la inicial del nombre para el avatar de respaldo
            InicialNombre = !string.IsNullOrEmpty(barbero.Nombre) ?
                           barbero.Nombre[..1].ToUpper() : "?";

            // Configurar la imagen
            ImagenPath = barbero.ImagenPath;
            TieneImagen = !string.IsNullOrEmpty(barbero.ImagenPath);
            NoTieneImagen = !TieneImagen;

            // Configurar calificación
            if (barbero.TotalCalificaciones > 0)
            {
                CalificacionTexto = $"★ {barbero.CalificacionPromedio:F1} ({barbero.TotalCalificaciones} reseñas)";
            }
            else
            {
                CalificacionTexto = "Sin calificaciones";
            }
        }
    }
}
