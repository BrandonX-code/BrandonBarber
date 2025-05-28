using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Gasolutions.Maui.App.Services;
using Gasolutions.Maui.App.Models;
using System.Diagnostics;


namespace Gasolutions.Maui.App.Pages
{
    public partial class GaleriaPage : ContentPage
    {
        // Lista para almacenar las imágenes
        private List<ImagenGaleriaModel> imagenes = new List<ImagenGaleriaModel>();
        private readonly GaleriaService _galeriaService;

        public GaleriaPage(GaleriaService galeriaService)
        {
            InitializeComponent();
            _galeriaService = galeriaService;

            // Mostrar/ocultar el botón basado en el rol
            if (AuthService.CurrentUser?.Rol?.ToLower() != "barbero")
            {
                // Ocultar botón si no es barbero
                var addButton = this.FindByName<Button>("AgregarImagenButton");
                if (addButton != null)
                    addButton.IsVisible = false;
            }

            LoadGaleria();
        }


        // Método para cargar imágenes desde la API
        private async void LoadGaleria()
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
                    await DisplayAlert("Sin conexión", "Verifica tu conexión a internet", "OK");
                    return;
                }

                // Obtener imágenes desde la API
                imagenes = await _galeriaService.ObtenerImagenes();

                Debug.WriteLine($"📷 Se obtuvieron {imagenes.Count} imágenes de la API");

                // Log detallado de cada imagen
                foreach (var imagen in imagenes)
                {
                    Debug.WriteLine($"🖼️ Imagen ID: {imagen.Id}");
                    Debug.WriteLine($"📄 Nombre: {imagen.NombreArchivo}");
                    Debug.WriteLine($"📂 Ruta: {imagen.RutaArchivo}");

                    var imageUrl = $"{_galeriaService.BaseUrl.TrimEnd('/')}{imagen.RutaArchivo}";
                    Debug.WriteLine($"🔗 URL completa: {imageUrl}");

                    // Verificar si la URL es accesible
                    try
                    {
                        using var httpClient = new HttpClient();
                        httpClient.Timeout = TimeSpan.FromSeconds(5);
                        var response = await httpClient.GetAsync(imageUrl);
                        Debug.WriteLine($"📡 Status de {imagen.NombreArchivo}: {response.StatusCode}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Error verificando {imagen.NombreArchivo}: {ex.Message}");
                    }
                }

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
                WidthRequest = 160
            };

            // Crear un contenedor para la imagen
            var imageContainer = new Grid
            {
                WidthRequest = 160,
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

            // Botón para eliminar (solo visible para barberos)
            Button deleteButton = new Button
            {
                Text = "✕",
                FontSize = 16,
                TextColor = Colors.White,
                BackgroundColor = Colors.Red,
                CornerRadius = 15,
                WidthRequest = 30,
                HeightRequest = 30,
                Padding = new Thickness(0),
                VerticalOptions = LayoutOptions.Start,
                HorizontalOptions = LayoutOptions.End,
                Margin = new Thickness(5),
                IsVisible = esBarbero
            };

            deleteButton.Clicked += async (sender, e) => await DeleteImage(imagen.Id);

            // Agregar elementos al contenedor de imagen
            imageContainer.Children.Add(imageControl);
            imageContainer.Children.Add(placeholderLabel);
            imageContainer.Children.Add(deleteButton);

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
                    await DisplayAlert("Éxito", "Imagen eliminada correctamente.", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo eliminar la imagen.", "OK");
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
                await DisplayAlert("Error", "La imagen no tiene ruta asignada", "OK");
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

                // Usar el selector de archivos para elegir una imagen
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Selecciona una imagen de corte de cabello",
                    FileTypes = FilePickerFileType.Images
                });
                if (AuthService.CurrentUser?.Rol?.ToLower() != "barbero")
                {
                    await DisplayAlert("Acceso Denegado", "Solo los barberos pueden subir imágenes.", "OK");
                    return;
                }
                if (result != null)
                {
                    // Pedir descripción opcional
                    string descripcion = await DisplayPromptAsync("Descripción", "Ingresa una descripción para la imagen (opcional):",
                        placeholder: "Ej: Corte fade con barba", maxLength: 500);

                    // Subir la imagen a la API
                    bool subida = await _galeriaService.SubirImagen(result.FullPath, descripcion);

                    if (subida)
                    {
                        await DisplayAlert("Éxito", "Imagen subida correctamente.", "OK");
                        // Recargar la galería
                        await LoadGaleriaAsync();
                    }
                    else
                    {
                        await DisplayAlert("Error", "No se pudo subir la imagen.", "OK");
                    }
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
                imagenes = await _galeriaService.ObtenerImagenes();
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