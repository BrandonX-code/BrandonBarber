using Barber.Maui.BrandonBarber.Controls;

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
        private int _barberiaSeleccionadaIndex = -1;
        bool _isUpdatingText = false;
        private bool _isNavigating = false;
        private bool _barberiaPickerLocked = false;

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
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                long idAdministrador = AuthService.CurrentUser!.Cedula;
                _barberias = await _barberiaService!.GetBarberiasByAdministradorAsync(idAdministrador);
                PickerSection.IsVisible = _barberias.Any();
                BarberiaSelectButton.IsVisible = _barberias.Count > 1;

                // Mostrar botón cambiar solo si hay más de 1 barbería
                var cambiarButton = this.FindByName<Button>("BarberiaSelectButton");
                if (cambiarButton != null)
                {
                    cambiarButton.IsVisible = _barberias.Count > 1;
                    cambiarButton.Text = "Seleccionar";
                }

                if (_barberias.Count > 0)
                {
                    _barberiaSeleccionadaIndex = 0;
                    var barberia = _barberias[0];
                    _barberiaSeleccionadaId = barberia.Idbarberia;
                    BarberiaSelectedLabel.Text = barberia.Nombre ?? "Seleccionar Barbería";
                    BarberiaTelefonoLabel.Text = barberia.Telefono ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(barberia.LogoUrl))
                    {
                        BarberiaLogoImage.Source = barberia.LogoUrl.StartsWith("http")
                            ? ImageSource.FromUri(new Uri(barberia.LogoUrl))
                            : ImageSource.FromFile(barberia.LogoUrl);
                    }
                    else
                    {
                        BarberiaLogoImage.Source = "picture.png";
                    }
                    // Cambiar texto del botón a "Cambiar" porque ya hay una barbería seleccionada
                    if (cambiarButton != null)
                    {
                        cambiarButton.Text = "Cambiar";
                    }
                    // Mostrar servicios de la barbería seleccionada por defecto
                    await CargarServicios(_barberiaSeleccionadaId);
                }
                else
                {
                    _barberiaSeleccionadaId = 0;
                    BarberiaSelectedLabel.Text = "Seleccionar Barbería";
                    BarberiaTelefonoLabel.Text = string.Empty;
                    BarberiaLogoImage.Source = "picture.png";
                    // Limpiar servicios si no hay barberías
                    ServiciosListView.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudieron cargar las barberías: " + ex.Message, "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }

        private async void OnBarberiaPickerTapped(object sender, EventArgs e)
        {
            // Previene doble clic
            if (_barberiaPickerLocked) return;
            _barberiaPickerLocked = true;

            try
            {
                if (_barberias == null || _barberias.Count <= 1)
                    return;

                var popup = new BarberiaSelectionPopup(_barberias);
                var seleccionada = await popup.ShowAsync();

                if (seleccionada != null)
                {
                    int idx = _barberias.FindIndex(b => b.Idbarberia == seleccionada.Idbarberia);
                    if (idx >= 0)
                    {
                        _barberiaSeleccionadaIndex = idx;
                        _barberiaSeleccionadaId = seleccionada.Idbarberia;

                        BarberiaSelectedLabel.Text = seleccionada.Nombre ?? "Seleccionar Barbería";
                        BarberiaTelefonoLabel.Text = seleccionada.Telefono ?? string.Empty;

                        if (!string.IsNullOrWhiteSpace(seleccionada.LogoUrl))
                        {
                            BarberiaLogoImage.Source = seleccionada.LogoUrl.StartsWith("http")
                                ? ImageSource.FromUri(new Uri(seleccionada.LogoUrl))
                                : ImageSource.FromFile(seleccionada.LogoUrl);
                        }
                        else
                        {
                            BarberiaLogoImage.Source = "picture.png";
                        }

                        var cambiarButton = this.FindByName<Button>("BarberiaSelectButton");
                        if (cambiarButton != null)
                            cambiarButton.Text = "Cambiar";

                        // Cargar los servicios de la nueva barbería
                        await CargarServicios(_barberiaSeleccionadaId);

                        // Si el servicio editado ya no existe, limpiar formulario
                        if (_servicioEditando != null)
                        {
                            var servicios = ServiciosListView.ItemsSource as IEnumerable<ServicioModel>;
                            if (servicios == null || !servicios.Any(s => s.Id == _servicioEditando.Id))
                                LimpiarFormulario();
                        }
                    }
                }
                else if (_barberiaSeleccionadaIndex >= 0 && _barberias.Count > _barberiaSeleccionadaIndex)
                {
                    var barberia = _barberias[_barberiaSeleccionadaIndex];
                    _barberiaSeleccionadaId = barberia.Idbarberia;

                    BarberiaSelectedLabel.Text = barberia.Nombre ?? "Seleccionar Barbería";
                    BarberiaTelefonoLabel.Text = barberia.Telefono ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(barberia.LogoUrl))
                    {
                        BarberiaLogoImage.Source = barberia.LogoUrl.StartsWith("http")
                            ? ImageSource.FromUri(new Uri(barberia.LogoUrl))
                            : ImageSource.FromFile(barberia.LogoUrl);
                    }
                    else
                    {
                        BarberiaLogoImage.Source = "picture.png";
                    }

                    await CargarServicios(_barberiaSeleccionadaId);

                    if (_servicioEditando != null)
                    {
                        var servicios = ServiciosListView.ItemsSource as IEnumerable<ServicioModel>;
                        if (servicios == null || !servicios.Any(s => s.Id == _servicioEditando.Id))
                            LimpiarFormulario();
                    }
                }
            }
            finally
            {
                _barberiaPickerLocked = false; // vuelve a habilitar el botón
            }
        }

        private async Task CargarServicios(int idBarberia)
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                var servicios = await _servicioService.GetServiciosByBarberiaAsync(idBarberia);
                ServiciosListView.ItemsSource = servicios;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudieron cargar los servicios: " + ex.Message, "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }

        private async void OnAgregarServicio(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                if (string.IsNullOrWhiteSpace(NombreEntry.Text) || _imagenSeleccionada == null || string.IsNullOrWhiteSpace(PrecioEntry.Text))
                {
                    _ = AppUtils.MostrarSnackbar("Todos los campos son obligatorios", Colors.Red, Colors.White);
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
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
                _isNavigating = false;
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
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                if ((sender as Button)?.CommandParameter is not ServicioModel servicio) return;

                _servicioEditando = servicio;
                NombreEntry.Text = servicio.Nombre;
                PrecioEntry.Text = servicio.Precio.ToString();
            
                // Mostrar la imagen previa si existe
                if (!string.IsNullOrEmpty(servicio.Imagen))
                {
                    PreviewImage.Source = servicio.Imagen;
                    PreviewImageBorder.HeightRequest = 100;
                    PreviewImageBorder.WidthRequest = 100;
                    PreviewImageBorder.IsVisible = true;
                }
                else
                {
                    PreviewImageBorder.HeightRequest = 0;
                    PreviewImageBorder.WidthRequest = 0;
                    PreviewImageBorder.IsVisible = false;
                }

                _imagenSeleccionada = null; // Se debe volver a seleccionar si se quiere cambiar

                AgregarBtn.IsVisible = false;
                EditarBtn.IsVisible = true;
                CancelarBtn.IsVisible = true;
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
                _isNavigating = false;
            }
        }

        private async void OnEditarServicio(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                if (_servicioEditando == null) return;

                string precioTexto = PrecioEntry.Text?.Replace("$", "").Replace(",", "")!;
                if (!decimal.TryParse(precioTexto, out decimal precio))
                {
                    await DisplayAlert("Validación", "El precio debe ser un número válido.", "OK");
                    return;
                }

                string imagenPath = _servicioEditando.Imagen!;

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
                    await CargarServicios(_barberiaSeleccionadaId);
                    _ = AppUtils.MostrarSnackbar("Servicio Editado Con Exito", Colors.Green, Colors.White);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", "No se pudo editar el servicio: " + ex.Message, "OK");
                }
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
                _isNavigating = false;
            }
        }

        private async void OnEliminarBtnClicked(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;
                if ((sender as Button)?.CommandParameter is not ServicioModel servicio) return;
                var popup = new CustomAlertPopup($"¿Quieres Eliminar el servicio ' {servicio.Nombre}'?");
                bool confirm = await popup.ShowAsync(this);
                if (!confirm) return;

                try
                {
                    _ = AppUtils.MostrarSnackbar("Servicio Eliminado Con Exito", Colors.Green, Colors.White);
                    await _servicioService.EliminarServicioAsync(servicio.Id);
                    await CargarServicios(_barberiaSeleccionadaId); // Pasar el ID de la barbería
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", "No se pudo eliminar el servicio: " + ex.Message, "OK");
                }
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
                _isNavigating = false;
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
            PreviewImageBorder.HeightRequest = 0;
            PreviewImageBorder.WidthRequest = 0;
            PreviewImageBorder.IsVisible = false;
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
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                try
                {
                    LoadingIndicator.IsVisible = true;
                    LoadingIndicator.IsLoading = true;
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
                        PreviewImageBorder.HeightRequest = 100;
                        PreviewImageBorder.WidthRequest = 100;
                        PreviewImageBorder.IsVisible = true;
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", "No se pudo seleccionar la imagen: " + ex.Message, "OK");
                }
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
                _isNavigating = false;
            }
        }
    }
}