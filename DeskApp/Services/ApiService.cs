using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DeskApp.Configuration;
using DeskApp.Models;

namespace DeskApp.Services
{
    public class ApiService
    {
        private static ApiService? _instance;
        private static readonly object _lock = new object();
        private readonly HttpClient _httpClient;
        private readonly AppConfiguration _config;

        private ApiService()
        {
            _config = AppConfiguration.Instance;
            _httpClient = new HttpClient();
            
            // Configurar headers necesarios para ngrok
            _httpClient.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public static ApiService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ApiService();
                        }
                    }
                }
                return _instance;
            }
        }

        public async Task<ApiResult<UserRegistrationResponse>> RegisterUserAsync(UserRegistrationRequest request)
        {
            var result = new ApiResult<UserRegistrationResponse>();

            try
            {
                var url = _config.GetFullUrl(_config.UsersEndpoint);
                
                var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                result.StatusCode = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    // Respuesta exitosa (200-299)
                    try
                    {
                        var successResponse = JsonSerializer.Deserialize<UserRegistrationResponse>(
                            responseContent,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                        result.Success = true;
                        result.Data = successResponse;
                    }
                    catch (JsonException)
                    {
                        result.Success = false;
                        result.ErrorMessage = "Error al procesar la respuesta del servidor";
                    }
                }
                else
                {
                    // Error del servidor (400, 404, 500, etc.)
                    result.Success = false;
                    
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(
                            responseContent,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                        if (errorResponse != null)
                        {
                            result.ErrorMessage = errorResponse.Message;
                            result.ValidationErrors = errorResponse.Errors;

                            // Si no hay errores específicos pero hay un campo "error"
                            if (errorResponse.Errors == null && !string.IsNullOrEmpty(errorResponse.Error))
                            {
                                result.ValidationErrors = new System.Collections.Generic.List<string> { errorResponse.Error };
                            }
                        }
                        else
                        {
                            result.ErrorMessage = $"Error HTTP {result.StatusCode}";
                        }
                    }
                    catch (JsonException)
                    {
                        // Si no se puede deserializar como error estructurado, usar el contenido raw
                        result.ErrorMessage = !string.IsNullOrEmpty(responseContent) 
                            ? responseContent 
                            : $"Error HTTP {result.StatusCode}";
                    }
                }
            }
            catch (TaskCanceledException)
            {
                result.Success = false;
                result.StatusCode = 0;
                result.ErrorMessage = "La solicitud ha excedido el tiempo de espera";
            }
            catch (HttpRequestException ex)
            {
                result.Success = false;
                result.StatusCode = 0;
                result.ErrorMessage = $"Error de conexión: {ex.Message}";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.StatusCode = 0;
                result.ErrorMessage = $"Error inesperado: {ex.Message}";
            }

            return result;
        }

        public async Task<ApiResult<LoginResponse>> LoginAsync(LoginRequest request)
        {
            var result = new ApiResult<LoginResponse>();

            try
            {
                var url = _config.GetFullUrl(_config.LoginEndpoint);
                
                var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                result.StatusCode = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    // Respuesta exitosa (200-299)
                    try
                    {
                        var successResponse = JsonSerializer.Deserialize<LoginResponse>(
                            responseContent,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                        result.Success = true;
                        result.Data = successResponse;
                    }
                    catch (JsonException)
                    {
                        result.Success = false;
                        result.ErrorMessage = "Error al procesar la respuesta del servidor";
                    }
                }
                else
                {
                    // Error del servidor (400, 401, 404, 500, etc.)
                    result.Success = false;
                    
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(
                            responseContent,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                        if (errorResponse != null)
                        {
                            result.ErrorMessage = errorResponse.Message;
                            result.ValidationErrors = errorResponse.Errors;

                            if (errorResponse.Errors == null && !string.IsNullOrEmpty(errorResponse.Error))
                            {
                                result.ValidationErrors = new System.Collections.Generic.List<string> { errorResponse.Error };
                            }
                        }
                        else
                        {
                            result.ErrorMessage = $"Error HTTP {result.StatusCode}";
                        }
                    }
                    catch (JsonException)
                    {
                        result.ErrorMessage = !string.IsNullOrEmpty(responseContent) 
                            ? responseContent 
                            : $"Error HTTP {result.StatusCode}";
                    }
                }
            }
            catch (TaskCanceledException)
            {
                result.Success = false;
                result.StatusCode = 0;
                result.ErrorMessage = "La solicitud ha excedido el tiempo de espera";
            }
            catch (HttpRequestException ex)
            {
                result.Success = false;
                result.StatusCode = 0;
                result.ErrorMessage = $"Error de conexión: {ex.Message}";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.StatusCode = 0;
                result.ErrorMessage = $"Error inesperado: {ex.Message}";
            }

            return result;
        }

        public async Task<ApiResult<List<UserData>>> GetUsersAsync(string bearerToken)
        {
            var result = new ApiResult<List<UserData>>();

            try
            {
                var url = _config.GetFullUrl(_config.UsersEndpoint);
                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                if (string.IsNullOrWhiteSpace(bearerToken))
                {
                    result.Success = false;
                    result.StatusCode = 401;
                    result.ErrorMessage = "Token de autenticación no disponible";
                    return result;
                }

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                result.StatusCode = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var users = JsonSerializer.Deserialize<List<UserData>>(
                            responseContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        result.Success = true;
                        result.Data = users ?? new List<UserData>();
                    }
                    catch (JsonException)
                    {
                        result.Success = false;
                        result.ErrorMessage = "Error al procesar la respuesta del servidor";
                    }
                }
                else
                {
                    result.Success = false;
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(
                            responseContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (errorResponse != null)
                        {
                            result.ErrorMessage = errorResponse.Message;
                            result.ValidationErrors = errorResponse.Errors;
                            if (errorResponse.Errors == null && !string.IsNullOrEmpty(errorResponse.Error))
                            {
                                result.ValidationErrors = new System.Collections.Generic.List<string> { errorResponse.Error };
                            }
                        }
                        else
                        {
                            result.ErrorMessage = $"Error HTTP {result.StatusCode}";
                        }
                    }
                    catch (JsonException)
                    {
                        result.ErrorMessage = !string.IsNullOrEmpty(responseContent)
                            ? responseContent
                            : $"Error HTTP {result.StatusCode}";
                    }
                }
            }
            catch (TaskCanceledException)
            {
                result.Success = false;
                result.StatusCode = 0;
                result.ErrorMessage = "La solicitud ha excedido el tiempo de espera";
            }
            catch (HttpRequestException ex)
            {
                result.Success = false;
                result.StatusCode = 0;
                result.ErrorMessage = $"Error de conexión: {ex.Message}";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.StatusCode = 0;
                result.ErrorMessage = $"Error inesperado: {ex.Message}";
            }

            return result;
        }

        // Método para probar la conexión con la API
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var url = _config.ApiBaseUrl;
                var response = await _httpClient.GetAsync(url);
                return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound;
            }
            catch
            {
                return false;
            }
        }
    }
}
