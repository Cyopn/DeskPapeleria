using System;
using System.Collections.Generic;
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

        private static (string Message, List<string>? ValidationErrors) ExtractApiError(string? responseContent, int statusCode)
        {
            if (string.IsNullOrWhiteSpace(responseContent))
                return ($"Error HTTP {statusCode}", null);

            try
            {
                var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (errorResponse != null)
                {
                    var message = !string.IsNullOrWhiteSpace(errorResponse.Message)
                        ? errorResponse.Message
                        : (!string.IsNullOrWhiteSpace(errorResponse.Error)
                            ? errorResponse.Error
                            : $"Error HTTP {statusCode}");

                    var errors = errorResponse.Errors;
                    if ((errors == null || errors.Count == 0) && !string.IsNullOrWhiteSpace(errorResponse.Error))
                    {
                        errors = new List<string> { errorResponse.Error };
                    }

                    return (message, errors);
                }
            }
            catch
            {
            }

            try
            {
                using var doc = JsonDocument.Parse(responseContent);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                    {
                        var m = msg.GetString();
                        if (!string.IsNullOrWhiteSpace(m)) return (m!, null);
                    }

                    if (doc.RootElement.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.String)
                    {
                        var e = err.GetString();
                        if (!string.IsNullOrWhiteSpace(e)) return (e!, null);
                    }
                }
            }
            catch
            {
            }

            return (responseContent, null);
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
                    var parsed = ExtractApiError(responseContent, result.StatusCode);
                    result.ErrorMessage = parsed.Message;
                    result.ValidationErrors = parsed.ValidationErrors;
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
                    var parsed = ExtractApiError(responseContent, result.StatusCode);
                    result.ErrorMessage = parsed.Message;
                    result.ValidationErrors = parsed.ValidationErrors;
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
                    var parsed = ExtractApiError(responseContent, result.StatusCode);
                    result.ErrorMessage = parsed.Message;
                    result.ValidationErrors = parsed.ValidationErrors;
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
                    var parsed = ExtractApiError(responseContent, result.StatusCode);
                    result.ErrorMessage = parsed.Message;
                    result.ValidationErrors = parsed.ValidationErrors;
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
                    var parsed = ExtractApiError(responseContent, result.StatusCode);
                    result.ErrorMessage = parsed.Message;
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
                            result.ValidationErrors = null;
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
                    var parsed = ExtractApiError(responseContent, result.StatusCode);
                    result.ErrorMessage = parsed.Message;
                    result.ValidationErrors = parsed.ValidationErrors;
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
                    var parsed = ExtractApiError(responseContent, result.StatusCode);
                    result.ErrorMessage = parsed.Message;
                    result.ValidationErrors = parsed.ValidationErrors;
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
                    var parsed = ExtractApiError(responseContent, result.StatusCode);
                    result.ErrorMessage = parsed.Message;
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

                using var message = new HttpRequestMessage(HttpMethod.Post, url)
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
                    var parsed = ExtractApiError(responseContent, result.StatusCode);
                    result.ErrorMessage = parsed.Message;
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

        public async Task<ApiResult<List<PrinterData>>> GetPrintersAsync(string bearerToken)
        {
            var result = new ApiResult<List<PrinterData>>();

            try
            {
                var url = _config.GetFullUrl("/printers");
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
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

                        using var doc = JsonDocument.Parse(responseContent);
                        var root = doc.RootElement;

                        List<PrinterData>? parsed = null;

                        if (root.ValueKind == JsonValueKind.Object)
                        {
                            if (root.TryGetProperty("printers", out var printersProp) && printersProp.ValueKind == JsonValueKind.Array)
                            {
                                parsed = JsonSerializer.Deserialize<List<PrinterData>>(printersProp.GetRawText(), options);
                            }
                            else if (root.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Array)
                            {
                                parsed = JsonSerializer.Deserialize<List<PrinterData>>(dataProp.GetRawText(), options);
                            }
                            else if (root.TryGetProperty("printer", out var printerProp) && printerProp.ValueKind == JsonValueKind.Object)
                            {
                                var single = JsonSerializer.Deserialize<PrinterData>(printerProp.GetRawText(), options);
                                if (single != null) parsed = new List<PrinterData> { single };
                            }
                        }
                        else if (root.ValueKind == JsonValueKind.Array)
                        {
                            parsed = JsonSerializer.Deserialize<List<PrinterData>>(responseContent, options);
                        }

                        result.Success = true;
                        result.Data = parsed ?? new List<PrinterData>();
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
                    var parsed = ExtractApiError(responseContent, result.StatusCode);
                    result.ErrorMessage = parsed.Message;
                    result.ValidationErrors = parsed.ValidationErrors;
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

        public async Task<ApiResult<List<TransactionData>>> GetTransactionsAsync(string bearerToken)
        {
            var result = new ApiResult<List<TransactionData>>();

            try
            {
                var url = _config.GetFullUrl("/transactions");
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
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    using var doc = JsonDocument.Parse(responseContent);
                    var root = doc.RootElement;
                    List<TransactionData>? parsed = null;

                    if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("transactions", out var txs) && txs.ValueKind == JsonValueKind.Array)
                    {
                        parsed = JsonSerializer.Deserialize<List<TransactionData>>(txs.GetRawText(), options);
                    }
                    else if (root.ValueKind == JsonValueKind.Array)
                    {
                        parsed = JsonSerializer.Deserialize<List<TransactionData>>(responseContent, options);
                    }
                    else if (root.ValueKind == JsonValueKind.Object)
                    {
                        var single = JsonSerializer.Deserialize<TransactionData>(responseContent, options);
                        if (single != null) parsed = new List<TransactionData> { single };
                    }

                    result.Success = true;
                    result.Data = parsed ?? new List<TransactionData>();
                }
                else
                {
                    result.Success = false;
                    var parsed = ExtractApiError(responseContent, result.StatusCode);
                    result.ErrorMessage = parsed.Message;
                    result.ValidationErrors = parsed.ValidationErrors;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.StatusCode = 0;
                result.ErrorMessage = $"Error inesperado: {ex.Message}";
            }

            return result;
        }

        public async Task<ApiResult<TransactionData>> GetTransactionByIdAsync(int transactionId, string bearerToken)
        {
            var result = new ApiResult<TransactionData>();

            try
            {
                var url = _config.GetFullUrl($"/transactions/{transactionId}");
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
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    using var doc = JsonDocument.Parse(responseContent);
                    var root = doc.RootElement;

                    TransactionData? tx = null;
                    if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("transaction", out var txProp) && txProp.ValueKind == JsonValueKind.Object)
                    {
                        tx = JsonSerializer.Deserialize<TransactionData>(txProp.GetRawText(), options);
                    }
                    else if (root.ValueKind == JsonValueKind.Object)
                    {
                        tx = JsonSerializer.Deserialize<TransactionData>(responseContent, options);
                    }

                    if (tx == null)
                    {
                        result.Success = false;
                        result.ErrorMessage = "No se pudo interpretar el detalle de la transacción";
                    }
                    else
                    {
                        result.Success = true;
                        result.Data = tx;
                    }
                }
                else
                {
                    result.Success = false;
                    var parsed = ExtractApiError(responseContent, result.StatusCode);
                    result.ErrorMessage = parsed.Message;
                    result.ValidationErrors = parsed.ValidationErrors;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.StatusCode = 0;
                result.ErrorMessage = $"Error inesperado: {ex.Message}";
            }

            return result;
        }

        public async Task<ApiResult<TransactionData>> CompleteTransactionAsync(int transactionId, string bearerToken)
        {
            var result = new ApiResult<TransactionData>();

            try
            {
                var url = _config.GetFullUrl($"/transactions/{transactionId}/complete");
                using var request = new HttpRequestMessage(HttpMethod.Patch, url);

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
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        using var doc = JsonDocument.Parse(responseContent);

                        TransactionData? tx = null;
                        if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                            doc.RootElement.TryGetProperty("transaction", out var txElement) &&
                            txElement.ValueKind == JsonValueKind.Object)
                        {
                            tx = JsonSerializer.Deserialize<TransactionData>(txElement.GetRawText(), options);
                        }
                        else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            tx = JsonSerializer.Deserialize<TransactionData>(responseContent, options);
                        }

                        result.Success = true;
                        result.Data = tx;
                    }
                    catch (JsonException)
                    {
                        result.Success = true;
                        result.Data = null;
                    }
                }
                else
                {
                    result.Success = false;
                    var parsed = ExtractApiError(responseContent, result.StatusCode);
                    result.ErrorMessage = parsed.Message;
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

        public async Task<ApiResult<bool>> UpdateTransactionFilesStatusAsync(int transactionId, string status, string bearerToken)
        {
            var result = new ApiResult<bool>();

            try
            {
                var url = _config.GetFullUrl($"/transactions/{transactionId}/files/status");

                if (string.IsNullOrWhiteSpace(bearerToken))
                {
                    result.Success = false;
                    result.StatusCode = 401;
                    result.ErrorMessage = "Token de autenticación no disponible";
                    return result;
                }

                var payload = JsonSerializer.Serialize(new { status });
                using var request = new HttpRequestMessage(HttpMethod.Patch, url)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                var response = await _httpClient.SendAsync(request);
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
                    var parsed = ExtractApiError(responseContent, result.StatusCode);
                    result.ErrorMessage = parsed.Message;
                    result.ValidationErrors = parsed.ValidationErrors;
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

        public async Task<ApiResult<bool>> UpdateProductSpecialServiceStatusAsync(int productId, string status, string bearerToken)
        {
            var result = new ApiResult<bool>();

            try
            {
                var url = _config.GetFullUrl($"/products/{productId}/special-service/status");

                if (string.IsNullOrWhiteSpace(bearerToken))
                {
                    result.Success = false;
                    result.StatusCode = 401;
                    result.ErrorMessage = "Token de autenticación no disponible";
                    return result;
                }

                var payload = JsonSerializer.Serialize(new { status });
                using var request = new HttpRequestMessage(HttpMethod.Patch, url)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                var response = await _httpClient.SendAsync(request);
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
                    var parsed = ExtractApiError(responseContent, result.StatusCode);
                    result.ErrorMessage = parsed.Message;
                    result.ValidationErrors = parsed.ValidationErrors;
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

        public async Task<ApiResult<PrinterData>> CreatePrinterAsync(PrinterData requestObj, string bearerToken)
        {
            var result = new ApiResult<PrinterData>();
            try
            {
                var url = _config.GetFullUrl("/printers");
                if (string.IsNullOrWhiteSpace(bearerToken))
                {
                    result.Success = false;
                    result.StatusCode = 401;
                    result.ErrorMessage = "Token de autenticación no disponible";
                    return result;
                }

                var jsonContent = JsonSerializer.Serialize(requestObj, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
                });

                using var message = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                var response = await _httpClient.SendAsync(message);
                var responseContent = await response.Content.ReadAsStringAsync();
                result.StatusCode = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseContent);
                    if (doc.RootElement.TryGetProperty("printer", out var printerElement) && printerElement.ValueKind == JsonValueKind.Object)
                    {
                        result.Data = JsonSerializer.Deserialize<PrinterData>(printerElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    else
                    {
                        result.Data = JsonSerializer.Deserialize<PrinterData>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    result.Success = true;
                }
                else
                {
                    result.Success = false;
                    var parsed = ExtractApiError(responseContent, result.StatusCode);
                    result.ErrorMessage = parsed.Message;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.StatusCode = 0;
                result.ErrorMessage = $"Error inesperado: {ex.Message}";
            }
            return result;
        }

        public async Task<ApiResult<PrinterData>> UpdatePrinterAsync(int printerId, PrinterData requestObj, string bearerToken)
        {
            var result = new ApiResult<PrinterData>();
            try
            {
                var baseUrl = _config.GetFullUrl("/printers");
                var url = baseUrl.EndsWith("/") ? $"{baseUrl}{printerId}" : $"{baseUrl}/{printerId}";

                if (string.IsNullOrWhiteSpace(bearerToken))
                {
                    result.Success = false;
                    result.StatusCode = 401;
                    result.ErrorMessage = "Token de autenticación no disponible";
                    return result;
                }

                var jsonContent = JsonSerializer.Serialize(requestObj, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
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
                    result.Success = true;
                    result.Data = JsonSerializer.Deserialize<PrinterData>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                else
                {
                    result.Success = false;
                    var parsed = ExtractApiError(responseContent, result.StatusCode);
                    result.ErrorMessage = parsed.Message;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.StatusCode = 0;
                result.ErrorMessage = $"Error inesperado: {ex.Message}";
            }
            return result;
        }

        public async Task<ApiResult<bool>> DeletePrinterAsync(int printerId, string bearerToken)
        {
            var result = new ApiResult<bool>();
            try
            {
                var baseUrl = _config.GetFullUrl("/printers");
                var url = baseUrl.EndsWith("/") ? $"{baseUrl}{printerId}" : $"{baseUrl}/{printerId}";
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
                    var parsed = ExtractApiError(responseContent, result.StatusCode);
                    result.ErrorMessage = parsed.Message;
                }
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
