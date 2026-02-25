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

        public async Task<ApiResult<UserData>> UpdateUserAsync(int userId, UserUpdateRequest request, string bearerToken)
        {
            var result = new ApiResult<UserData>();

            try
            {
                var baseUrl = _config.GetFullUrl(_config.UsersEndpoint);
                var url = baseUrl.EndsWith("/") ? $"{baseUrl}{userId}" : $"{baseUrl}/{userId}";

                if (string.IsNullOrWhiteSpace(bearerToken))
                {
                    result.Success = false;
                    result.StatusCode = 401;
                    result.ErrorMessage = "Token de autenticación no disponible";
                    return result;
                }

                var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                using var message = new HttpRequestMessage(HttpMethod.Put, url)
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                var response = await _httpClient.SendAsync(message);
                var responseContent = await response.Content.ReadAsStringAsync();

                result.StatusCode = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var updatedUser = JsonSerializer.Deserialize<UserData>(
                            responseContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        result.Success = true;
                        result.Data = updatedUser;
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

        public async Task<ApiResult<bool>> DeleteUserAsync(int userId, string bearerToken)
        {
            var result = new ApiResult<bool>();

            try
            {
                var baseUrl = _config.GetFullUrl(_config.UsersEndpoint);
                var url = baseUrl.EndsWith("/") ? $"{baseUrl}{userId}" : $"{baseUrl}/{userId}";

                if (string.IsNullOrWhiteSpace(bearerToken))
                {
                    result.Success = false;
                    result.StatusCode = 401;
                    result.ErrorMessage = "Token de autenticación no disponible";
                    return result;
                }

                using var message = new HttpRequestMessage(HttpMethod.Delete, url);
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                var response = await _httpClient.SendAsync(message);
                var responseContent = await response.Content.ReadAsStringAsync();

                result.StatusCode = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    result.Success = true;
                    result.Data = true;
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

        public async Task<ApiResult<List<ProductData>>> GetProductsByTypeAsync(string type, string bearerToken)
        {
            var result = new ApiResult<List<ProductData>>();

            try
            {
                var baseUrl = _config.GetFullUrl(_config.UsersEndpoint);
                var url = _config.GetFullUrl($"/products/type/{type}");

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

                try { Console.WriteLine($"[ApiService] GET {url} - Response: {responseContent}"); } catch {  }

                result.StatusCode = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(responseContent);
                        if (doc.RootElement.TryGetProperty("products", out var productsElement) && productsElement.ValueKind == JsonValueKind.Array)
                        {
                            var products = JsonSerializer.Deserialize<List<ProductData>>(productsElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            result.Success = true;
                            result.Data = products ?? new List<ProductData>();
                        }
                        else if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            var products = JsonSerializer.Deserialize<List<ProductData>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            result.Success = true;
                            result.Data = products ?? new List<ProductData>();
                        }
                        else
                        {
                            result.Success = false;
                            result.ErrorMessage = "Respuesta de productos no contiene la propiedad 'products'";
                        }
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

        public async Task<ApiResult<ProductData>> UpdateProductAsync(int productId, ProductUpdateRequest request, string bearerToken)
        {
            var result = new ApiResult<ProductData>();

            try
            {
                var baseUrl = _config.GetFullUrl("/products");
                var url = baseUrl.EndsWith("/") ? $"{baseUrl}{productId}" : $"{baseUrl}/{productId}";

                if (string.IsNullOrWhiteSpace(bearerToken))
                {
                    result.Success = false;
                    result.StatusCode = 401;
                    result.ErrorMessage = "Token de autenticación no disponible";
                    return result;
                }

                var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                try { Console.WriteLine($"[ApiService] PUT {url} - Request JSON: {jsonContent}"); } catch { }

                using var message = new HttpRequestMessage(HttpMethod.Put, url)
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                
                try
                {
                    Console.WriteLine($"[ApiService] PUT {url} - Authorization present: {message.Headers.Authorization != null}");
                    Console.WriteLine($"[ApiService] PUT {url} - Content-Type: {message.Content.Headers.ContentType}");
                    Console.WriteLine($"[ApiService] PUT {url} - Content Length: {message.Content.Headers.ContentLength}");
                    System.Diagnostics.Debug.WriteLine($"[ApiService] PUT {url} - Full request:\nHeaders: {string.Join("; ", message.Headers)}\nContent: {jsonContent}");
                }
                catch { }

                var response = await _httpClient.SendAsync(message);
                var responseContent = await response.Content.ReadAsStringAsync();

                try { Console.WriteLine($"[ApiService] PUT {url} - Status: {(int)response.StatusCode} - Response: {responseContent}"); } catch { }

                result.StatusCode = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(responseContent);
                        if (doc.RootElement.TryGetProperty("product", out var productElement))
                        {
                            var product = JsonSerializer.Deserialize<ProductData>(productElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            result.Success = true;
                            result.Data = product;
                        }
                        else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            var product = JsonSerializer.Deserialize<ProductData>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            result.Success = true;
                            result.Data = product;
                        }
                        else
                        {
                            result.Success = false;
                            result.ErrorMessage = "Respuesta inesperada del servidor al actualizar producto";
                        }
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

        public async Task<ApiResult<bool>> DeleteProductAsync(int productId, string bearerToken)
        {
            var result = new ApiResult<bool>();

            try
            {
                var baseUrl = _config.GetFullUrl("/products");
                var url = baseUrl.EndsWith("/") ? $"{baseUrl}{productId}" : $"{baseUrl}/{productId}";

                if (string.IsNullOrWhiteSpace(bearerToken))
                {
                    result.Success = false;
                    result.StatusCode = 401;
                    result.ErrorMessage = "Token de autenticación no disponible";
                    return result;
                }

                using var message = new HttpRequestMessage(HttpMethod.Delete, url);
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                var response = await _httpClient.SendAsync(message);
                var responseContent = await response.Content.ReadAsStringAsync();

                result.StatusCode = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    result.Success = true;
                    result.Data = true;
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

        public async Task<ApiResult<ProductData>> CreateProductAsync(ProductCreateRequest request, string bearerToken)
        {
            var result = new ApiResult<ProductData>();

            try
            {
                var url = _config.GetFullUrl("/products");

                if (string.IsNullOrWhiteSpace(bearerToken))
                {
                    result.Success = false;
                    result.StatusCode = 401;
                    result.ErrorMessage = "Token de autenticación no disponible";
                    return result;
                }

                var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                try { Console.WriteLine($"[ApiService] POST {url} - Request: {jsonContent}"); } catch { }

                using var message = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                var response = await _httpClient.SendAsync(message);
                var responseContent = await response.Content.ReadAsStringAsync();

                try { Console.WriteLine($"[ApiService] POST {url} - Status: {(int)response.StatusCode} - Response: {responseContent}"); } catch { }

                result.StatusCode = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(responseContent);
                        if (doc.RootElement.TryGetProperty("product", out var productElement))
                        {
                            var product = JsonSerializer.Deserialize<ProductData>(productElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            result.Success = true;
                            result.Data = product;
                        }
                        else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            var product = JsonSerializer.Deserialize<ProductData>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            result.Success = true;
                            result.Data = product;
                        }
                        else
                        {
                            result.Success = false;
                            result.ErrorMessage = "Respuesta inesperada del servidor al crear producto";
                        }
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
    }
}
