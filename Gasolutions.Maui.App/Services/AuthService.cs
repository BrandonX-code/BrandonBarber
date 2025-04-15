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
        private readonly HttpClient _BaseClient;
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

                var response = await _BaseClient.PostAsync("auth/login", content);

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔹 Login Response: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = $"Error: {response.StatusCode} - {responseContent}"
                    };
                }

                var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (authResponse.Success)
                {
                    CurrentUser = authResponse.User;

                    // Guardar el token para futuras peticiones
                    await SecureStorage.Default.SetAsync("auth_token", authResponse.Token);
                    // ya está bien con sólo:
                    await SecureStorage.Default.SetAsync("user_cedula", CurrentUser.Cedula.ToString());

                    // Configurar el token en el HttpClient para futuras peticiones
                    _BaseClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", authResponse.Token);
                }

                return authResponse;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error en login: {ex.Message}");
                return new AuthResponse
                {
                    Success = false,
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

                Console.WriteLine($"🔹 Enviando solicitud a {_BaseClient.BaseAddress}auth/register");
                Console.WriteLine($"🔹 Datos enviados: {json}");

                var responseContent = await _BaseClient.PostAsync("auth/register", content);
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
                    await Application.Current.MainPage.DisplayAlert("Éxito", "¡Registro exitoso! Ahora puedes iniciar sesión.", "Aceptar");
                    return new AuthResponse { IsSuccess = true, Message = "Registro exitoso." };
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

                if (string.IsNullOrEmpty(token))
                    return false;

                // Configurar el token en el HttpClient
                _BaseClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                // Verificar si el token es válido haciendo una petición al servidor
                var response = await _BaseClient.GetAsync($"{_BaseClient}validate");

                if (!response.IsSuccessStatusCode)
                {
                    // Token inválido, hacer logout
                    await Logout();
                    return false;
                }

                // Obtener información del usuario
                var userCedula = await SecureStorage.Default.GetAsync("user_cedula");

                if (string.IsNullOrEmpty(userCedula) || string.IsNullOrEmpty(userCedula))
                {
                    await Logout();
                    return false;
                }

                // Obtener los datos del usuario
                var userResponse = await _BaseClient.GetAsync($"{_BaseClient}auth/user/{userCedula}");

                if (!userResponse.IsSuccessStatusCode)
                {
                    await Logout();
                    return false;
                }

                var responseContent = await userResponse.Content.ReadAsStringAsync();
                CurrentUser = JsonSerializer.Deserialize<UsuarioModels>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error al verificar autenticación: {ex.Message}");
                await Logout();
                return false;
            }
        }
    }
}