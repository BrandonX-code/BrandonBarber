using Gasolutions.Maui.App.Models;
using Gasolutions.Maui.App.Services;
using Microsoft.Maui.Storage; // Asegúrate de tener este using

namespace Gasolutions.Maui.App.Pages
{
    public partial class GestionarServiciosPage : ContentPage
    {
        private readonly ServicioService _servicioService;
        private ServicioModel _servicioEditando;
        private FileResult _imagenSeleccionada;

        public GestionarServiciosPage(ServicioService servicioService)
        {
            InitializeComponent();
            _servicioService = servicioService;
            CargarServicios();
        }

        private async void CargarServicios()
        {
            try
            {
                var servicios = await _servicioService.GetServiciosAsync();
                ServiciosListView.ItemsSource = servicios;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudieron cargar los servicios: " + ex.Message, "OK");
            }
        }

        private async void OnAgregarServicio(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NombreEntry.Text) || _imagenSeleccionada == null || string.IsNullOrWhiteSpace(PrecioEntry.Text))
            {
                await DisplayAlert("Validación", "Todos los campos son obligatorios.", "OK");
                return;
            }

            if (!decimal.TryParse(PrecioEntry.Text, out decimal precio))
            {
                await DisplayAlert("Validación", "El precio debe ser un número válido.", "OK");
                return;
            }

            // Guardar la imagen en el almacenamiento local de la app
            string fileName = Path.GetFileName(_imagenSeleccionada.FullPath);
            string localPath = Path.Combine(FileSystem.AppDataDirectory, fileName);

            using (var stream = await _imagenSeleccionada.OpenReadAsync())
            using (var fileStream = File.OpenWrite(localPath))
            {
                await stream.CopyToAsync(fileStream);
            }

            var nuevoServicio = new ServicioModel
            {
                Nombre = NombreEntry.Text,
                Imagen = localPath, // Guarda la ruta local
                Precio = precio
            };

            try
            {
                await _servicioService.CrearServicioAsync(nuevoServicio);
                LimpiarFormulario();
                CargarServicios();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo agregar el servicio: " + ex.Message, "OK");
            }
        }
        bool _isUpdatingText = false;

        private void OnPrecioEntryTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingText || sender is not Entry entry) return;

            string raw = e.NewTextValue?.Replace("$", "").Replace(",", "") ?? "";

            if (decimal.TryParse(raw, out decimal valor))
            {
                string formatted = string.Format("${0:N0}", valor);

                if (entry.Text != formatted)
                {
                    _isUpdatingText = true;

                    entry.Dispatcher.Dispatch(() =>
                    {
                        entry.Text = formatted;
                        entry.CursorPosition = formatted.Length;
                        _isUpdatingText = false;
                    });
                }
            }
        }


        private void OnEditarBtnClicked(object sender, EventArgs e)
        {
            var servicio = (sender as Button)?.CommandParameter as ServicioModel;
            if (servicio == null) return;

            _servicioEditando = servicio;
            NombreEntry.Text = servicio.Nombre;
            PrecioEntry.Text = servicio.Precio.ToString();

            // Mostrar la imagen previa si existe
            if (!string.IsNullOrEmpty(servicio.Imagen))
            {
                PreviewImage.Source = servicio.Imagen;
                PreviewImage.IsVisible = true;
            }
            else
            {
                PreviewImage.IsVisible = false;
            }

            _imagenSeleccionada = null; // Se debe volver a seleccionar si se quiere cambiar

            AgregarBtn.IsVisible = false;
            EditarBtn.IsVisible = true;
            CancelarBtn.IsVisible = true;
        }

        private async void OnEditarServicio(object sender, EventArgs e)
        {
            if (_servicioEditando == null) return;

            if (!decimal.TryParse(PrecioEntry.Text, out decimal precio))
            {
                await DisplayAlert("Validación", "El precio debe ser un número válido.", "OK");
                return;
            }

            string imagenPath = _servicioEditando.Imagen;

            if (_imagenSeleccionada != null)
            {
                string fileName = Path.GetFileName(_imagenSeleccionada.FullPath);
                string localPath = Path.Combine(FileSystem.AppDataDirectory, fileName);

                using (var stream = await _imagenSeleccionada.OpenReadAsync())
                using (var fileStream = File.OpenWrite(localPath))
                {
                    await stream.CopyToAsync(fileStream);
                }
                imagenPath = localPath;
            }

            _servicioEditando.Nombre = NombreEntry.Text;
            _servicioEditando.Imagen = imagenPath;
            _servicioEditando.Precio = precio;

            try
            {
                await _servicioService.EditarServicioAsync(_servicioEditando);
                LimpiarFormulario();
                CargarServicios();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo editar el servicio: " + ex.Message, "OK");
            }
        }

        private async void OnEliminarBtnClicked(object sender, EventArgs e)
        {
            var servicio = (sender as Button)?.CommandParameter as ServicioModel;
            if (servicio == null) return;

            bool confirm = await DisplayAlert("Confirmar", $"¿Eliminar el servicio '{servicio.Nombre}'?", "Sí", "No");
            if (!confirm) return;

            try
            {
                await _servicioService.EliminarServicioAsync(servicio.Id);
                CargarServicios();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo eliminar el servicio: " + ex.Message, "OK");
            }
        }

        private void OnCancelarEdicion(object sender, EventArgs e)
        {
            LimpiarFormulario();
        }

        private void LimpiarFormulario()
        {
            NombreEntry.Text = "";
            PrecioEntry.Text = "";
            PreviewImage.Source = null;
            PreviewImage.IsVisible = false;
            _imagenSeleccionada = null;
            _servicioEditando = null;
            AgregarBtn.IsVisible = true;
            EditarBtn.IsVisible = false;
            CancelarBtn.IsVisible = false;
        }

        private void OnServicioSeleccionado(object sender, SelectionChangedEventArgs e)
        {
            ServiciosListView.SelectedItem = null; // Para evitar selección persistente
        }

        private async void OnSeleccionarImagen(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Selecciona una imagen",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    _imagenSeleccionada = result;

                    // Crear una copia del stream en memoria
                    using var stream = await result.OpenReadAsync();
                    var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    // Asignar la imagen y hacerla visible
                    PreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(memoryStream.ToArray()));
                    PreviewImage.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo seleccionar la imagen: " + ex.Message, "OK");
            }
        }
    }
}