using Gasolutions.Maui.App.Models;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Gasolutions.Maui.App.Services
{
    public class AuthService
    {
        public readonly HttpClient _BaseClient;
        private string URLServices;
        public static UsuarioModels CurrentUser { get; set; }

        public AuthService(HttpClient httpClient)
        {
            _BaseClient = httpClient;
            URLServices = _BaseClient.BaseAddress.ToString();
        }

        public async Task<AuthResponse> Login(string email, string password)
        {
            try
            {
                var loginRequest = new LoginRequest
                {
                    Email = email,
                    Contraseña = password
                };

                var json = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _BaseClient.PostAsync("api/auth/login", content);

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔹 Login Response: {responseContent}");
                Console.WriteLine($"🔹 Status Code: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    return new AuthResponse
                    {
                        IsSuccess = false,
                        Message = $"Error: {response.StatusCode} - {responseContent}"
                    };
                }

                var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                Console.WriteLine($"🔹 AuthResponse IsSuccess: {authResponse?.IsSuccess}");
                Console.WriteLine($"🔹 AuthResponse Message: {authResponse?.Message}");

                if (authResponse != null && authResponse.IsSuccess)
                {
                    CurrentUser = authResponse.User;

                    // Guardar el token para futuras peticiones
                    await SecureStorage.Default.SetAsync("auth_token", authResponse.Token);
                    await SecureStorage.Default.SetAsync("user_cedula", CurrentUser.Cedula.ToString());

                    // Configurar el token en el HttpClient para futuras peticiones
                    _BaseClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", authResponse.Token);

                    Console.WriteLine($"🔹 Usuario logueado: {CurrentUser.Nombre} - Rol: {CurrentUser.Rol}");
                }

                return authResponse ?? new AuthResponse { IsSuccess = false, Message = "Respuesta vacía del servidor" };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error en login: {ex.Message}");
                Console.WriteLine($"❌ Error en login: {ex.Message}");
                return new AuthResponse
                {
                    IsSuccess = false,
                    Message = $"Error de conexión: {ex.Message}"
                };
            }
        }

        public async Task<AuthResponse> Register(RegistroRequest registroRequest)
        {
            try
            {
                var json = JsonSerializer.Serialize(registroRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"🔹 Enviando solicitud a {_BaseClient.BaseAddress}api/auth/register");
                Console.WriteLine($"🔹 Datos enviados: {json}");

                var responseContent = await _BaseClient.PostAsync("api/auth/register", content);
                string responseMessage = await responseContent.Content.ReadAsStringAsync();
                Console.WriteLine($"🔹 Código de estado API: {responseContent.StatusCode}");
                Console.WriteLine($"🔹 Respuesta API: {responseMessage}");

                // Verificar la respuesta dependiendo del código de estado
                if (responseContent.StatusCode == HttpStatusCode.Conflict)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "El usuario ya existe. Por favor, elija otro correo electrónico.", "Aceptar");
                    return new AuthResponse { IsSuccess = false, Message = "El usuario ya existe." };
                }

                if (responseContent.StatusCode == HttpStatusCode.BadRequest)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Los datos enviados no son válidos. Por favor, verifique los campos e intente nuevamente.", "Aceptar");
                    return new AuthResponse { IsSuccess = false, Message = "Datos inválidos." };
                }

                if (responseContent.StatusCode == HttpStatusCode.InternalServerError)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Hubo un problema en el servidor. Por favor, intente más tarde.", "Aceptar");
                    return new AuthResponse { IsSuccess = false, Message = "Error del servidor." };
                }

                if (responseContent.IsSuccessStatusCode)
                {
                    return new AuthResponse { IsSuccess = true, Message = "¡Registro exitoso! Ahora puedes iniciar sesión." };
                }

                // Si el código de estado no es específico
                await Application.Current.MainPage.DisplayAlert("Error", "Hubo un error al procesar tu solicitud. Intenta nuevamente.", "Aceptar");
                return new AuthResponse { IsSuccess = false, Message = $"Error desconocido. Código de estado: {responseContent.StatusCode}" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al conectar con la API: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error de Conexión", "Hubo un problema al intentar conectar con el servidor. Por favor, verifica tu conexión a internet.", "Aceptar");
                return new AuthResponse { IsSuccess = false, Message = "Error de conexión." };
            }
        }

        public async Task<bool> Logout()
        {
            try
            {
                // Eliminar token del almacenamiento seguro
                SecureStorage.Default.Remove("auth_token");
                SecureStorage.Default.Remove("user_cedula");

                // Limpiar headers de autorización
                _BaseClient.DefaultRequestHeaders.Authorization = null;
                CurrentUser = null;

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error en logout: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CheckAuthStatus()
        {
            try
            {
                var token = await SecureStorage.Default.GetAsync("auth_token");
                var userCedula = await SecureStorage.Default.GetAsync("user_cedula");

                Console.WriteLine($"🔹 Token almacenado: {(string.IsNullOrEmpty(token) ? "No encontrado" : "Encontrado")}");
                Console.WriteLine($"🔹 Cedula almacenada: {userCedula ?? "No encontrada"}");

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userCedula))
                {
                    Console.WriteLine("🔹 No hay token o cedula almacenados");
                    return false;
                }

                // Configurar el token en el HttpClient
                _BaseClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                // Obtener los datos del usuario directamente
                var userResponse = await _BaseClient.GetAsync($"api/auth/user/{userCedula}");

                Console.WriteLine($"🔹 Respuesta del servidor para usuario: {userResponse.StatusCode}");

                if (!userResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine("🔹 No se pudo obtener datos del usuario, haciendo logout");
                    await Logout();
                    return false;
                }

                var responseContent = await userResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"🔹 Datos del usuario obtenidos: {responseContent}");

                CurrentUser = JsonSerializer.Deserialize<UsuarioModels>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (CurrentUser != null)
                {
                    Console.WriteLine($"🔹 Usuario cargado: {CurrentUser.Nombre} - Rol: {CurrentUser.Rol}");
                    return true;
                }

                await Logout();
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al verificar autenticación: {ex.Message}");
                Console.WriteLine($"❌ Error al verificar autenticación: {ex.Message}");
                await Logout();
                return false;
            }
        }
        public async Task<bool> EliminarUsuario(long cedula)
        {
            var response = await _BaseClient.DeleteAsync($"api/auth/{cedula}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error al eliminar usuario: {error}");
            }

            return true;
        }
        public async Task<List<UsuarioModels>> ObtenerBarberos()
        {
            try
            {
                var response = await _BaseClient.GetAsync("api/auth/barberos");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var barberos = JsonSerializer.Deserialize<List<UsuarioModels>>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return barberos ?? new List<UsuarioModels>();
                }
                return new List<UsuarioModels>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al obtener barberos: {ex.Message}");
                return new List<UsuarioModels>();
            }
        }

        //public async Task<UsuarioModels> GetUserByCedula(long cedula)
        //{
        //    try
        //    {
        //        var response = await _BaseClient.GetAsync($"api/auth/usuario/{cedula}");
        //        if (response.IsSuccessStatusCode)
        //        {
        //            var content = await response.Content.ReadAsStringAsync();
        //            var usuario = JsonSerializer.Deserialize<UsuarioModels>(content,
        //                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        //            // Filtrar solo clientes
        //            if (usuario != null && usuario.Rol?.ToLower() == "cliente")
        //            {
        //                return usuario;
        //            }
        //        }
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Error al obtener cliente: {ex.Message}");
        //        return null;
        //    }
        //}
        public async Task<UsuarioModels> GetUserByCedula(long cedula)
        {
            try
            {
                var response = await _BaseClient.GetAsync($"api/auth/usuario/{cedula}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<UsuarioModels>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al obtener usuario: {ex.Message}");
                return null;
            }
        }
    }
}