using Gasolutions.Maui.App.Models;
using Gasolutions.Maui.App.Services;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;


namespace Gasolutions.Maui.App.Pages
{
    public partial class GaleriaPage : ContentPage
    {
        private readonly AuthService _authService;
        private List<UsuarioModels> _todosLosBarberos;
        public List<UsuarioModels> TodosLosBarberos
        {
            get => _todosLosBarberos;
            set
            {
                _todosLosBarberos = value;
                OnPropertyChanged(nameof(TodosLosBarberos)); // Asegúrate de implementar INotifyPropertyChanged
            }
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Solo cargar barberos si es cliente
            if (AuthService.CurrentUser?.Rol?.ToLower() != "barbero")
            {
                _ = LoadBarberos();
            }
        }
        // Lista para almacenar las imágenes
        private List<ImagenGaleriaModel> imagenes = new List<ImagenGaleriaModel>();
        private readonly GaleriaService _galeriaService;

        public GaleriaPage(GaleriaService galeriaService, AuthService barberoid)
        {
            InitializeComponent();
            _galeriaService = galeriaService;
            _authService = barberoid;

            // Determinar si el usuario es barbero o cliente
            bool esBarbero = AuthService.CurrentUser?.Rol?.ToLower() == "barbero";

            if (esBarbero)
            {
                // Si es barbero: ocultar picker y mostrar solo su galería
                var pickerSection = this.FindByName<VerticalStackLayout>("PickerSection");
                if (pickerSection != null)
                    pickerSection.IsVisible = false;

                // Cargar la galería del barbero actual
                LoadGaleria();
            }
            else
            {
                // Si es cliente: mostrar picker y ocultar botón de agregar
                var pickerSection = this.FindByName<VerticalStackLayout>("PickerSection");
                if (pickerSection != null)
                    pickerSection.IsVisible = true;

                var addButton = this.FindByName<Button>("AgregarImagenButton");
                if (addButton != null)
                    addButton.IsVisible = false;
            }
        }
        private async void Picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            var picker = (Picker)sender;
            int selectedIndex = picker.SelectedIndex;
            if (selectedIndex != -1)
            {
                UsuarioModels barbero = (UsuarioModels)picker.SelectedItem;
                LoadGaleria(barbero.Cedula);
            }
        }
        private async Task LoadBarberos()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                var response = await _authService._BaseClient.GetAsync("api/auth");

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var usuarios = System.Text.Json.JsonSerializer.Deserialize<List<UsuarioModels>>(jsonContent,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var barberos = usuarios?.Where(u => u.Rol.ToLower() == "barbero").ToList() ?? new List<UsuarioModels>();
                    TodosLosBarberos = barberos;
                    Picker.ItemsSource = TodosLosBarberos;
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
                LoadingIndicator.IsRunning = false;
            }
        }
        // Método para cargar imágenes desde la API
        private async void LoadGaleria(long barberoId = 0)
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

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
                long cedulaBarbero = barberoId == 0 ? AuthService.CurrentUser.Cedula : barberoId;
                imagenes = await _galeriaService.ObtenerImagenes(cedulaBarbero);

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
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        // Método para actualizar la interfaz de la galería
        private void UpdateGaleriaUI()
        {
            Debug.WriteLine($"🖼️ Actualizando UI con {imagenes.Count} imágenes");

            // Limpiar el contenedor
            GaleriaContainer.Children.Clear();

            // Si no hay imágenes, mostrar un mensaje
            if (imagenes.Count == 0)
            {
                var emptyLabel = new Label
                {
                    Text = "No hay imágenes en la galería. Agrega tu primer corte.",
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
        private Frame CreateImageFrame(ImagenGaleriaModel imagen)
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
            var imageUrl = $"{_galeriaService.BaseUrl.TrimEnd('/')}{imagen.RutaArchivo}";

            Debug.WriteLine($"🔗 URL final de imagen: {imageUrl}");

            // Crear la imagen con manejo de errores
            Image imageControl = new Image
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
                    BackgroundColor = Color.FromArgb("#2a2a2a"),
                    Padding = new Thickness(8, 4)
                };

                // Aplicar esquinas redondeadas a la descripción
                var descripcionFrame = new Frame
                {
                    Content = descripcionLabel,
                    BackgroundColor = Color.FromArgb("#2a2a2a"),
                    BorderColor = Color.FromArgb("black"),
                    CornerRadius = 5,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0),
                    HasShadow = false
                };

                mainContainer.Children.Add(descripcionFrame);
            }

            // Frame contenedor principal
            var frame = new Frame
            {
                Content = mainContainer,
                CornerRadius = 10,
                Padding = new Thickness(8),
                Margin = new Thickness(5, 5, 5, 15),
                IsClippedToBounds = true,
                BorderColor = Color.FromArgb("black"),
                HasShadow = true,
                BackgroundColor = Color.FromArgb("#1e1e1e")
            };

            // Gesto para ver detalle
            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += async (s, e) => await ShowImageDetail(imagen);
            imageControl.GestureRecognizers.Add(tapGestureRecognizer);

            return frame;
        }
        private async Task EditarDescripcionImagen(ImagenGaleriaModel imagen)
        {
            string nuevaDescripcion = await DisplayPromptAsync(
                "Editar descripción",
                "Modifica la descripción de la imagen:",
                initialValue: imagen.Descripcion ?? "",
                maxLength: 500
            );

            if (nuevaDescripcion == null) // Cancelado
                return;

            bool actualizado = await _galeriaService.ActualizarImagen(imagen.Id, nuevaDescripcion);

            if (actualizado)
            {
                await AppUtils.MostrarSnackbar("Descripción actualizada.", Colors.Green, Colors.White);
                imagen.Descripcion = nuevaDescripcion;
                UpdateGaleriaUI();
            }
            else
            {
                await AppUtils.MostrarSnackbar("No se pudo actualizar la descripción.", Colors.Red, Colors.White);
            }
        }
        // Método para eliminar una imagen
        private async Task DeleteImage(int imagenId)
        {
            try
            {
                bool confirmar = await DisplayAlert("Confirmar", "¿Estás seguro de que deseas eliminar esta imagen?", "Sí", "No");

                if (!confirmar)
                    return;

                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

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
                LoadingIndicator.IsRunning = false;
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
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                // Solo barberos
                if (AuthService.CurrentUser?.Rol?.ToLower() != "barbero")
                {
                    await DisplayAlert("Acceso Denegado", "Solo los barberos pueden subir imágenes.", "OK");
                    return;
                }

                // Abre la galería
                var foto = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Selecciona una imagen de corte de cabello"
                });

                if (foto == null)
                    return; // usuario canceló

                // Opcional: pide descripción
                string descripcion = await DisplayPromptAsync("Descripción","Ingresa una descripción para la imagen (opcional):", placeholder: "Ej: Corte fade con barba",maxLength: 500
                );

                // Obtén la ruta o el stream para subir
                string rutaLocal = foto.FullPath;
                // o bien
                // using var stream = await foto.OpenReadAsync();

                // Subir la imagen a la API
                long barberoId = AuthService.CurrentUser.Cedula;
                bool subida = await _galeriaService.SubirImagen(rutaLocal, descripcion, barberoId);
                // si tu API acepta stream, pásalo en lugar de la ruta:
                // bool subida = await _galeriaService.SubirImagen(stream, descripcion);

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
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        // Método auxiliar para recargar la galería
        private async Task LoadGaleriaAsync()
        {
            try
            {
                Debug.WriteLine("🔄 Recargando galería...");
                long barberoId = AuthService.CurrentUser.Cedula;
                imagenes = await _galeriaService.ObtenerImagenes(barberoId);
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