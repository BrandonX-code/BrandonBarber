namespace Barber.Maui.BrandonBarber.Pages
{
    using Barber.Maui.BrandonBarber.Controls;
    public partial class GaleriaPage : ContentPage
    {
        private bool _isNavigating = false;
        private readonly AuthService _authService;
        private List<UsuarioModels>? _todosLosBarberos;
        private long _barberoSeleccionadoId = 0;
        private bool _primeraVez = true;
        public List<UsuarioModels>? TodosLosBarberos
        {
            get => _todosLosBarberos;
            set
            {
                _todosLosBarberos = value;
                OnPropertyChanged(nameof(TodosLosBarberos));
            }
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (AuthService.CurrentUser?.Rol?.ToLower() != "barbero")
            {
                if (_primeraVez)
                {
                    _ = LoadBarberiosYSeleccionarPrimero();
                    _primeraVez = false;
                }
            }
        }
        private List<ImagenGaleriaModel> imagenes = [];
        private readonly GaleriaService _galeriaService;

        public GaleriaPage(GaleriaService galeriaService, AuthService barberoid)
        {
            InitializeComponent();
            _galeriaService = galeriaService;
            _authService = barberoid;
            BarberoFotoImage.Source = "usericons.png";
            bool esBarbero = AuthService.CurrentUser?.Rol?.ToLower() == "barbero";

            if (esBarbero)
            {
                var pickerSection = this.FindByName<VerticalStackLayout>("PickerSection");
                if (pickerSection != null) pickerSection.IsVisible = false;
                LoadGaleria();
            }
            else
            {
                var pickerSection = this.FindByName<VerticalStackLayout>("PickerSection");
                if (pickerSection != null) pickerSection.IsVisible = true;

                var addButton = this.FindByName<Button>("AgregarImagenButton");
                if (addButton != null) addButton.IsVisible = false;
            }
        }
        private async void OnBarberoPickerTapped(object sender, EventArgs e)
        {
            if (_todosLosBarberos == null || _todosLosBarberos.Count == 0)
                return;

            var popup = new BarberoSelectionPopup(_todosLosBarberos);
            var seleccionado = await popup.ShowAsync();

            if (seleccionado != null)
            {
                // Actualizar la UI con el barbero seleccionado
                BarberoSelectedLabel.Text = seleccionado.Nombre ?? "Seleccionar Barbero";
                BarberoTelefonoLabel.Text = seleccionado.Telefono ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(seleccionado.ImagenPath))
                {
                    BarberoFotoImage.Source = seleccionado.ImagenPath.StartsWith("http")
                       ? ImageSource.FromUri(new Uri(seleccionado.ImagenPath))
                        : ImageSource.FromFile(seleccionado.ImagenPath);
                }
                else
                {
                    BarberoFotoImage.Source = "usericons.png";
                }

                // Cambiar texto del botón a "Cambiar" en el hilo principal
                // Cambiar texto del botón a "Cambiar"
                // Guardar el barbero seleccionado
                _barberoSeleccionadoId = seleccionado.Cedula;

                // Cargar la galería del barbero seleccionado
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;

                try
                {
                    var admin = AuthService.CurrentUser;
                    int idBarberia = admin!.IdBarberia ?? 0;
                    imagenes = await _galeriaService.ObtenerImagenes(seleccionado.Cedula, idBarberia);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        UpdateGaleriaUI();
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Error al cargar galería del barbero: {ex.Message}");
                    await DisplayAlert("Error", $"No se pudo cargar la galería: {ex.Message}", "OK");
                }
                finally
                {
                    LoadingIndicator.IsVisible = false;
                    LoadingIndicator.IsLoading = false;
                }
            }
        }
        private async Task LoadBarberos()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;

                var response = await _authService._BaseClient.GetAsync("api/auth");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var usuarios = System.Text.Json.JsonSerializer.Deserialize<List<UsuarioModels>>(jsonContent,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var admin = AuthService.CurrentUser;
                    var barberos = usuarios?.Where(u => u.Rol!.ToLower() == "barbero" && u.IdBarberia == admin!.IdBarberia).ToList() ?? [];
                    TodosLosBarberos = barberos;
                }


                else
                {
                    await DisplayAlert("Error", "No se pudieron cargar los barberos", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al cargar los barberos: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsLoading = false;
            }
        }
        private async Task LoadBarberiosYSeleccionarPrimero()
        {
            await LoadBarberos();

            if (_todosLosBarberos != null && _todosLosBarberos.Count > 0)
            {
                // Seleccionar automáticamente el primer barbero
                var primerBarbero = _todosLosBarberos[0];

                BarberoSelectedLabel.Text = primerBarbero.Nombre ?? "Seleccionar Barbero";
                BarberoTelefonoLabel.Text = primerBarbero.Telefono ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(primerBarbero.ImagenPath))
                {
                    BarberoFotoImage.Source = primerBarbero.ImagenPath.StartsWith("http")
                       ? ImageSource.FromUri(new Uri(primerBarbero.ImagenPath))
                        : ImageSource.FromFile(primerBarbero.ImagenPath);
                }
                else
                {
                    BarberoFotoImage.Source = "usericons.png";
                }

                // Mostrar botón "Cambiar" solo si hay más de un barbero
                BarberoSelectButton.IsVisible = _todosLosBarberos.Count > 1;
                if (_todosLosBarberos.Count > 1)
                {
                    BarberoSelectButton.Text = "Cambiar";
                }
                // Ocultar label de instrucción si solo hay un barbero
                SeleccionarBarberoLabel.IsVisible = _todosLosBarberos.Count > 1;
                // Cargar la galería del primer barbero
                await LoadGaleriaAsync(primerBarbero.Cedula);
            }
        }
        // Método para cargar imágenes desde la API
        private async void LoadGaleria(long barberoId = 0)
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;

                Debug.WriteLine("🔄 Iniciando carga de galería...");
                Debug.WriteLine($"📱 Plataforma: {DeviceInfo.Platform}");
                Debug.WriteLine($"🌐 BaseUrl del servicio: {_galeriaService.BaseUrl}");

                // Verificar conectividad de red
                var networkAccess = Connectivity.NetworkAccess;
                Debug.WriteLine($"🌐 Estado de red: {networkAccess}");

                if (networkAccess != NetworkAccess.Internet)
                {
                    await AppUtils.MostrarSnackbar("Verifica tu conexión a internet", Colors.Orange, Colors.White);
                    return;
                }

                // Obtener imágenes desde la API
                var usuarioActual = AuthService.CurrentUser;
                // Si el usuario actual es barbero, usar su cédula
                // Si es admin/cliente y no se especifica barberoId, usar el primero disponible
                long cedulaBarbero;
                if (barberoId != 0)
                {
                    cedulaBarbero = barberoId;
                }
                else if (usuarioActual!.Rol?.ToLower() == "barbero")
                {
                    cedulaBarbero = usuarioActual.Cedula;
                }
                else
                {
                    // Para admin/cliente sin barberoId especificado
                    cedulaBarbero = _todosLosBarberos?.FirstOrDefault()?.Cedula ?? usuarioActual.Cedula;
                }
                int idBarberia = usuarioActual!.IdBarberia ?? 0;
                imagenes = await _galeriaService.ObtenerImagenes(cedulaBarbero, idBarberia);

                Debug.WriteLine($"📷 Se obtuvieron {imagenes.Count} imágenes de la API");

                UpdateGaleriaUI();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al cargar galería: {ex.Message}");
                Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
                await DisplayAlert("Error", $"No se pudieron cargar las imágenes: {ex.Message}", "OK");
            }
            finally
            {
                LoadingIndicator.IsLoading = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        // Método para actualizar la interfaz de la galería
        private void UpdateGaleriaUI()
        {
            Debug.WriteLine($"🖼️ Actualizando UI con {imagenes.Count} imágenes");
            // Ocultar label de instrucción si no hay imágenes
            InstruccionGaleriaLabel.IsVisible = imagenes.Count > 0;
            // Limpiar el contenedor
            GaleriaContainer.Children.Clear();

            // Si no hay imágenes, mostrar un mensaje
            if (imagenes.Count == 0)
            {
                var emptyLabel = new Label
                {
                    Text = "No hay imágenes en la galería.",
                    TextColor = Color.FromArgb("#909090"),
                    FontSize = 16,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Margin = new Thickness(20)
                };

                GaleriaContainer.Children.Add(emptyLabel);
                Debug.WriteLine("📝 Mostrado mensaje de galería vacía");
                return;
            }

            // Agregar cada imagen a la galería
            foreach (var imagen in imagenes)
            {
                var frame = CreateImageFrame(imagen);
                GaleriaContainer.Children.Add(frame);
                Debug.WriteLine($"➕ Agregada imagen: {imagen.NombreArchivo}");
            }
        }
        // Crear un frame para cada imagen
        private Border CreateImageFrame(ImagenGaleriaModel imagen)
        {
            // Determinar si el usuario actual es barbero
            bool esBarbero = AuthService.CurrentUser?.Rol?.ToLower() == "barbero";

            // Crear un contenedor principal que incluirá imagen y descripción
            var mainContainer = new StackLayout
            {
                Spacing = 8,
                WidthRequest = 150
            };

            // Crear un contenedor para la imagen
            var imageContainer = new Grid
            {
                WidthRequest = 150,
                HeightRequest = 200
            };

            // Construir la URL completa correctamente
            var imageUrl = $"{imagen.RutaArchivo}";

            Debug.WriteLine($"🔗 URL final de imagen: {imageUrl}");

            // Crear la imagen con manejo de errores
            Image imageControl = new()
            {
                Source = ImageSource.FromUri(new Uri(imageUrl)),
                Aspect = Aspect.AspectFill,
                BackgroundColor = Colors.LightGray
            };

            // Agregar manejo de errores para la carga de imagen
            imageControl.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == "IsLoading")
                {
                    var img = sender as Image;
                    Debug.WriteLine($"🖼️ Imagen {imagen.NombreArchivo} - IsLoading: {img?.IsLoading}");
                }
            };

            // Agregar placeholder o imagen por defecto en caso de error
            var placeholderLabel = new Label
            {
                Text = "📷",
                FontSize = 40,
                TextColor = Colors.Gray,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                IsVisible = false
            };

            // Mostrar placeholder si la imagen no carga
            imageControl.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == "Source")
                {
                    var img = sender as Image;
                    if (img?.Source == null)
                    {
                        placeholderLabel.IsVisible = true;
                        Debug.WriteLine($"❌ Error cargando imagen: {imagen.NombreArchivo}");
                    }
                }
            };

            // Contenedor de botones (eliminar y editar)
            var buttonContainer = new HorizontalStackLayout
            {
                Spacing = 4,
                Padding = new Thickness(4),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                IsVisible = esBarbero
            };

            // Botón eliminar
            var deleteButton = new ImageButton
            {
                Source = "delete.png",
                BackgroundColor = Colors.Transparent,
                WidthRequest = 24,
                HeightRequest = 24,
                Padding = new Thickness(0),
                Margin = new Thickness(2)
            };
            deleteButton.Clicked += async (s, e) => await DeleteImage(imagen.Id);

            // Agregar botones al contenedo
            buttonContainer.Children.Add(deleteButton);

            // Agregar elementos al contenedor de imagen
            imageContainer.Children.Add(imageControl);
            imageContainer.Children.Add(placeholderLabel);
            imageContainer.Children.Add(buttonContainer);

            // Agregar la imagen al contenedor principal
            mainContainer.Children.Add(imageContainer);

            // NUEVA FUNCIONALIDAD: Agregar descripción solo para clientes
            if (!esBarbero && !string.IsNullOrWhiteSpace(imagen.Descripcion))
            {
                var descripcionLabel = new Label
                {
                    Text = imagen.Descripcion,
                    TextColor = Colors.White,
                    FontSize = 15,
                    LineBreakMode = LineBreakMode.WordWrap,
                    MaxLines = 2,
                    HorizontalTextAlignment = TextAlignment.Center,
                    Margin = new Thickness(5, 0, 5, 5),
                    BackgroundColor = Color.FromArgb("#0E2A36"),
                    Padding = new Thickness(8, 4)
                };

                // Aplicar esquinas redondeadas a la descripción
                var border = new Border
                {
                    Content = descripcionLabel,
                    BackgroundColor = Color.FromArgb("#0E2A36"),
                    Padding = new Thickness(0),
                    Margin = new Thickness(0),
                    StrokeShape = new RoundRectangle
                    {
                        CornerRadius = 5
                    }
                };
                mainContainer.Children.Add(border);
            }

            // Frame contenedor principal
            var borderPrincipal = new Border
            {
                Content = mainContainer,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = 5
                },
                Padding = new Thickness(5),
                Margin = new Thickness(5, 5, 5, 15),
                BackgroundColor = Color.FromArgb("#B0BEC5")
            };

            // Gesto para ver detalle
            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += async (s, e) => await ShowImageDetail(imagen);
            imageControl.GestureRecognizers.Add(tapGestureRecognizer);

            return borderPrincipal;
        }

        // Método para eliminar una imagen
        private async Task DeleteImage(int imagenId)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                var popup = new CustomAlertPopup("¿Quieres Eliminar Esta Imagen?");
                bool confirmar = await popup.ShowAsync(this);

                if (!confirmar)
                    return;

                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;

                bool eliminada = await _galeriaService.EliminarImagen(imagenId);

                if (eliminada)
                {
                    // Remover de la lista local
                    imagenes.RemoveAll(i => i.Id == imagenId);
                    UpdateGaleriaUI();
                    await AppUtils.MostrarSnackbar("Imagen eliminada correctamente.", Colors.Green, Colors.White);
                }
                else
                {
                    await AppUtils.MostrarSnackbar("No se pudo eliminar la imagen.", Colors.Red, Colors.White);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al eliminar imagen: {ex.Message}");
                await DisplayAlert("Error", $"Error al eliminar la imagen: {ex.Message}", "OK");
            }
            finally
            {
                _isNavigating = false;
                LoadingIndicator.IsLoading = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        // Método para mostrar una imagen en detalle
        private async Task ShowImageDetail(ImagenGaleriaModel imagen)
        {
            Console.WriteLine($"Ruta de la imagen: {imagen.RutaArchivo}");

            if (string.IsNullOrWhiteSpace(imagen.RutaArchivo))
            {
                await AppUtils.MostrarSnackbar("La imagen no tiene ruta asignada", Colors.Red, Colors.White);
                return;
            }

            await Navigation.PushAsync(new DetalleImagenPage(imagen, _galeriaService.BaseUrl));
        }


        // Evento del botón para agregar una nueva imagen
        private async void OnAgregarImagenClicked(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsLoading = true;

                // Solo barberos
                if (AuthService.CurrentUser?.Rol?.ToLower() != "barbero")
                {
                    await DisplayAlert("Acceso Denegado", "Solo los barberos pueden subir imágenes.", "OK");
                    return;
                }

                // Validar límite de imágenes
                if (imagenes.Count >= 6)
                {
                    await AppUtils.MostrarSnackbar("Solo puedes subir hasta 6 imágenes en tu galería.", Colors.Orange, Colors.White);
                    return;
                }

                // Abre la galería
                var foto = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Selecciona una imagen de corte de cabello"
                });

                if (foto == null)
                    return;

                var popup = new DescripcionImagenPopup();
                string? descripcion = await popup.ShowAsync();

                if (descripcion == null)
                {
                    await AppUtils.MostrarSnackbar("Operación cancelada.", Colors.Orange, Colors.White);
                    return;
                }

                // Si está vacío, usar string vacío
                descripcion = string.IsNullOrWhiteSpace(descripcion) ? string.Empty : descripcion;

                string rutaLocal = foto.FullPath;

                long barberoId = AuthService.CurrentUser.Cedula;
                bool subida = await _galeriaService.SubirImagen(rutaLocal, descripcion, barberoId);

                if (subida)
                {
                    await AppUtils.MostrarSnackbar("Imagen subida correctamente.", Colors.Green, Colors.White);
                    await LoadGaleriaAsync();
                }
                else
                {
                    await AppUtils.MostrarSnackbar("No se pudo subir la imagen.", Colors.Red, Colors.White);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al agregar imagen: {ex.Message}");
                await DisplayAlert("Error", $"No se pudo seleccionar la imagen: {ex.Message}", "OK");
            }
            finally
            {
                _isNavigating = false;
                LoadingIndicator.IsLoading = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        // Método auxiliar para recargar la galería
        private async Task LoadGaleriaAsync()
        {
            try
            {
                Debug.WriteLine("🔄 Recargando galería...");
                var usuarioActual = AuthService.CurrentUser;
                long barberoId = usuarioActual!.Cedula;
                int idBarberia = usuarioActual.IdBarberia ?? 0;
                imagenes = await _galeriaService.ObtenerImagenes(barberoId, idBarberia);
                UpdateGaleriaUI();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al recargar galería: {ex.Message}");
                await DisplayAlert("Error", $"No se pudieron recargar las imágenes: {ex.Message}", "OK");
            }
        }

        // Sobrecarga para cargar galería de un barbero específico
        private async Task LoadGaleriaAsync(long barberoId)
        {
            try
            {
                Debug.WriteLine($"🔄 Recargando galería del barbero {barberoId}...");
                var usuarioActual = AuthService.CurrentUser;
                int idBarberia = usuarioActual!.IdBarberia ?? 0;
                imagenes = await _galeriaService.ObtenerImagenes(barberoId, idBarberia);
                UpdateGaleriaUI();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al recargar galería: {ex.Message}");
                await DisplayAlert("Error", $"No se pudieron recargar las imágenes: {ex.Message}", "OK");
            }
        }

        // Método para refrescar la galería (pull to refresh)
        private async void OnRefreshGaleria(object sender, EventArgs e)
        {
            await LoadGaleriaAsync();
        }
    }
}