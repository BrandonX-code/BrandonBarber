namespace Barber.Maui.BrandonBarber.Pages
{
    public partial class GestionarServiciosPage : ContentPage
    {
        private readonly ServicioService _servicioService;
        private ServicioModel? _servicioEditando;
        private FileResult? _imagenSeleccionada;
        private readonly BarberiaService? _barberiaService;
        private List<Barberia>? _barberias;
        private int _barberiaSeleccionadaId;
        bool _isUpdatingText = false;

        public GestionarServiciosPage(ServicioService servicioService)
        {
            InitializeComponent();
            _servicioService = servicioService;
            _barberiaService = Application.Current!.Handler.MauiContext!.Services.GetService<BarberiaService>()!;

            CargarBarberias();
        }

        private async void CargarBarberias()
        {
            try
            {
                long idAdministrador = AuthService.CurrentUser.Cedula;
                _barberias = await _barberiaService!.GetBarberiasByAdministradorAsync(idAdministrador);

                Picker.ItemsSource = _barberias;
                Picker.ItemDisplayBinding = new Binding("Nombre");
                PickerSection.IsVisible = _barberias.Count != 0;

                if (_barberias.Count != 0)
                {
                    Picker.SelectedIndex = 0; // Dispara Picker_SelectedIndexChanged
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudieron cargar las barberías: " + ex.Message, "OK");
            }
        }

        private async void Picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            var picker = (Picker)sender;
            int selectedIndex = picker.SelectedIndex;
            if (selectedIndex != -1)
            {
                var barberiaSeleccionada = (Barberia)picker.SelectedItem;
                _barberiaSeleccionadaId = barberiaSeleccionada.Idbarberia;

                // Cargar los servicios de la nueva barbería
                await CargarServicios(_barberiaSeleccionadaId);

                // Si estamos editando, buscar el servicio equivalente en esta barbería
                if (_servicioEditando != null)
                {
                    var servicios = await _servicioService.GetServiciosByBarberiaAsync(_barberiaSeleccionadaId);
                    var servicioEnNuevaBarberia = servicios.FirstOrDefault(s => s.Id == _servicioEditando.Id);

                    if (servicioEnNuevaBarberia != null)
                    {
                        _servicioEditando = servicioEnNuevaBarberia;
                        NombreEntry.Text = _servicioEditando.Nombre;
                        PrecioEntry.Text = _servicioEditando.Precio.ToString("N0");

                        if (!string.IsNullOrEmpty(_servicioEditando.Imagen))
                        {
                            PreviewImage.Source = _servicioEditando.Imagen;
                            PreviewImage.IsVisible = true;
                        }
                        else
                        {
                            PreviewImage.IsVisible = false;
                        }

                        AgregarBtn.IsVisible = false;
                        EditarBtn.IsVisible = true;
                        CancelarBtn.IsVisible = true;
                    }
                    else
                    {
                        // Si el servicio no existe en esta barbería, limpiar
                        LimpiarFormulario();
                    }
                }
            }
        }


        private async Task CargarServicios(int idBarberia)
        {
            try
            {
                var servicios = await _servicioService.GetServiciosByBarberiaAsync(idBarberia);
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

            // Verificar que hay una barbería seleccionada
            if (_barberiaSeleccionadaId == 0)
            {
                await DisplayAlert("Validación", "Debe seleccionar una barbería.", "OK");
                return;
            }

            string precioTexto = PrecioEntry.Text?.Replace("$", "").Replace(",", "")!;
            if (!decimal.TryParse(precioTexto, out decimal precio))
            {
                await DisplayAlert("Validación", "El precio debe ser un número válido.", "OK");
                return;
            }

            // Guardar la imagen en el almacenamiento local de la app
            string fileName = System.IO.Path.GetFileName(_imagenSeleccionada.FullPath);
            string localPath = System.IO.Path.Combine(FileSystem.AppDataDirectory, fileName);

            using (var stream = await _imagenSeleccionada.OpenReadAsync())
            using (var fileStream = File.OpenWrite(localPath))
            {
                await stream.CopyToAsync(fileStream);
            }

            var nuevoServicio = new ServicioModel
            {
                Nombre = NombreEntry.Text,
                Imagen = localPath, // Guarda la ruta local
                Precio = precio,
                IdBarberia = _barberiaSeleccionadaId // Usar la barbería seleccionada
            };

            try
            {
                await _servicioService.CrearServicioAsync(nuevoServicio);
                LimpiarFormulario();
                await CargarServicios(_barberiaSeleccionadaId); // Pasar el ID de la barbería
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo agregar el servicio: " + ex.Message, "OK");
            }
        }
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
            if ((sender as Button)?.CommandParameter is not ServicioModel servicio) return;

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

            string precioTexto = PrecioEntry.Text?.Replace("$", "").Replace(",", "")!;
            if (!decimal.TryParse(precioTexto, out decimal precio))
            {
                await DisplayAlert("Validación", "El precio debe ser un número válido.", "OK");
                return;
            }

            string imagenPath = _servicioEditando.Imagen;

            if (_imagenSeleccionada != null)
            {
                string fileName = System.IO.Path.GetFileName(_imagenSeleccionada.FullPath);
                string localPath = System.IO.Path.Combine(FileSystem.AppDataDirectory, fileName);

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
                await CargarServicios(_barberiaSeleccionadaId); // Pasar el ID de la barbería
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo editar el servicio: " + ex.Message, "OK");
            }
        }

        private async void OnEliminarBtnClicked(object sender, EventArgs e)
        {
            if ((sender as Button)?.CommandParameter is not ServicioModel servicio) return;
            var popup = new CustomAlertPopup($"¿Quieres Eliminar el servicio '{servicio.Nombre}'?");
            bool confirm = await popup.ShowAsync(this);
            if (!confirm) return;

            try
            {
                await _servicioService.EliminarServicioAsync(servicio.Id);
                await CargarServicios(_barberiaSeleccionadaId); // Pasar el ID de la barbería
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