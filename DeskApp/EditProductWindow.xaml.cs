using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using System.Windows.Media.Imaging;
using System.Net.Http;
using System.IO;
using System.Net.Http.Headers;
using Microsoft.Win32;
using System.Text.Json;
using System.Text;
using DeskApp.Models;
using DeskApp.Services;
using DeskApp.Configuration;
using DeskApp.Extensions;

namespace DeskApp
{
    public partial class EditProductWindow : Window
    {
        private readonly ApiService _api_service;
        private readonly ApiService _apiService;
        public ProductData Product { get; set; }

        private byte[]? _pendingImageBytes;
        private string? _pendingImageFilename;
        private bool _imageChanged = false;

        public EditProductWindow(ProductData product)
        {
            try
            {
                InitializeComponent();
                _api_service = ApiService.Instance;
                _apiService = ApiService.Instance;
                Product = product ?? new ProductData();
                if (Product.File == null && Product.Files != null && Product.Files.Count > 0)
                {
                    Product.File = Product.Files.LastOrDefault();
                }

                this.DataContext = Product;

                if (PriceTextBox != null)
                {
                    PriceTextBox.LostFocus += (s, e) =>
                    {
                        try
                        {
                            var text = PriceTextBox.Text ?? string.Empty;
                            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var v))
                            {
                                PriceTextBox.Text = v.ToString("F2", CultureInfo.InvariantCulture);
                                Product.Price = v;
                            }
                        }
                        catch { }
                    };
                }

                var previewFile = Product.File ?? Product.Files?.LastOrDefault();
                if (previewFile != null)
                {
                    _ = ShowFilePreviewAsync(previewFile).ContinueWith(t => {
                        if (t.Exception != null)
                        {
                            try { System.Diagnostics.Debug.WriteLine($"[EditProduct] ShowFilePreview exception: {t.Exception}"); } catch { }
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
            }
            catch (Exception ex)
            {
                try { System.Diagnostics.Debug.WriteLine($"[EditProduct] Constructor exception: {ex}"); } catch { }
                try { System.Diagnostics.Debug.WriteLine($"[EditProduct] Constructor exception: {ex}"); } catch { }
                ToastNotification.Show($"Error al abrir editor: {ex.Message}", ToastType.Error, 4);
                this.DialogResult = false;
                this.Close();
            }
        }

        private async System.Threading.Tasks.Task ShowFilePreviewAsync(FileData file)
        {
            try
            {
                if (file == null) return;

                var baseUrl = AppConfiguration.Instance.ApiBaseUrl?.TrimEnd('/') ?? string.Empty;

                var typeStr = file.Type.ToString().ToLower();
                var url = $"{baseUrl}/file-manager/download/{typeStr}/{file.FileHash}";

                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(url, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    PreviewImage.Source = bitmap;
                    return;
                }
                catch
                {
                }

                var token = SessionService.Instance.AuthToken ?? string.Empty;
                using var http = new HttpClient();
                try { http.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true"); } catch { }

                http.DefaultRequestHeaders.Accept.Clear();
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));

                if (!string.IsNullOrEmpty(token))
                {
                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                using var resp = await http.GetAsync(url);
                if (!resp.IsSuccessStatusCode)
                {
                    PreviewImage.Source = null;
                    return;
                }

                using var contentStream = await resp.Content.ReadAsStreamAsync();
                using var ms = new MemoryStream();
                await contentStream.CopyToAsync(ms);
                ms.Position = 0;

                var bitmap2 = new BitmapImage();
                bitmap2.BeginInit();
                bitmap2.CacheOption = BitmapCacheOption.OnLoad;
                bitmap2.StreamSource = ms;
                bitmap2.EndInit();
                bitmap2.Freeze();
                try { PreviewImage.Source = bitmap2; } catch { }
            }
            catch
            {
                try { PreviewImage.Source = null; } catch { }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("¿Deseas guardar los cambios en este producto?", "Confirmar guardar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            SaveButton.IsEnabled = false;
            try
            {
                if (PriceTextBox != null)
                {
                    var text = PriceTextBox.Text ?? string.Empty;
                    if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var v))
                    {
                        Product.Price = v;
                    }
                }

                if (Product.IdProduct == 0)
                {
                    ToastNotification.Show("Producto sin ID. Usa la ventana de crear para crear un nuevo producto.", ToastType.Error, 4);
                    SaveButton.IsEnabled = true;
                    return;
                }

                var request = new ProductUpdateRequest
                {
                    Type = Product.Type,
                    Description = Product.Description,
                    Price = Product.Price,
                    IdFile = Product.IdFile
                };

                if (Product.Item != null)
                {
                    request.Item = new ItemUpdateRequest
                    {
                        Name = Product.Item.Name ?? string.Empty,
                        Available = Product.Item.Available,
                        Category = Product.Item.Category
                    };
                }

                if (Product.File != null)
                {
                    request.File = new FileUpdateRequest
                    {
                        Filename = Product.File.Filename,
                        Status = Product.File.Status,
                        Type = Product.File.Type,
                        FileHash = Product.File.FileHash
                    };
                }

                if (_pendingImageBytes != null && _imageChanged)
                {
                    var baseUrl = AppConfiguration.Instance.ApiBaseUrl?.TrimEnd('/') ?? string.Empty;
                    var service = "image";
                    var url = $"{baseUrl}/file-manager?service={service}";

                    using var http = new HttpClient();
                    http.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
                    var token = SessionService.Instance.AuthToken;
                    if (!string.IsNullOrEmpty(token)) http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    using var fd = new MultipartFormDataContent();
                    using var fs = new MemoryStream(_pendingImageBytes);
                    var fileName = System.IO.Path.GetFileName(_pendingImageFilename);
                    var streamContent = new StreamContent(fs);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    fd.Add(streamContent, "files", fileName);
                    fd.Add(new StringContent(SessionService.Instance.CurrentUser?.Username ?? ""), "username");

                    var resp = await http.PostAsync(url, fd);
                    var respText = await resp.Content.ReadAsStringAsync();
                    try { System.Diagnostics.Debug.WriteLine($"[Upload] POST {url} - Status: {(int)resp.StatusCode} - Response: {respText}"); } catch { }

                    if (!resp.IsSuccessStatusCode)
                    {
                        ToastNotification.Show("Error al subir imagen", ToastType.Error, 4);
                        return;
                    }

                    var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(respText);
                    var fileInfo = json.ValueKind == System.Text.Json.JsonValueKind.Array ? json[0] : json;

                    var originalName = fileInfo.GetPropertyOrDefault(new[] { "originalName", "originalname", "filename" })?.GetString() ?? fileName;
                    var storedName = fileInfo.GetPropertyOrDefault(new[] { "storedName", "storedname", "filename", "stored_file", "hash" })?.GetString() ?? string.Empty;
                    var returnedService = fileInfo.GetPropertyOrDefault(new[] { "service", "type" })?.GetString() ?? service;

                    var fileCreateReq = new FileCreateRequest
                    {
                        IdUser = SessionService.Instance.CurrentUser?.IdUser ?? 1,
                        Filename = originalName,
                        Type = ParseFileType(returnedService),
                        FileHash = storedName
                    };

                    var createResp = await CreateFileRecordAsync(fileCreateReq);
                    if (createResp == null)
                    {
                        ToastNotification.Show("Error al crear registro de archivo", ToastType.Error, 4);
                        return;
                    }

                    Product.File = new FileData
                    {
                        IdFile = createResp.IdFile,
                        IdUser = fileCreateReq.IdUser,
                        Filename = fileCreateReq.Filename,
                        FileHash = fileCreateReq.FileHash,
                        Type = fileCreateReq.Type,
                        Status = FileStatus.Active
                    };

                    Product.IdFile = createResp.IdFile;

                    try
                    {
                        request.IdFile = Product.IdFile;
                        request.File = new FileUpdateRequest
                        {
                            Filename = Product.File.Filename,
                            Status = Product.File.Status,
                            Type = Product.File.Type,
                            FileHash = Product.File.FileHash
                        };
                    }
                    catch { }
                }

                var token2 = SessionService.Instance.AuthToken ?? string.Empty;

                try
                {
                    var jsonPreserve = System.Text.Json.JsonSerializer.Serialize(request, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = null });
                    System.Diagnostics.Debug.WriteLine($"[EditProduct] PUT /products/{Product.IdProduct} - Request JSON (preserve): {jsonPreserve}");
                    var jsonCamel = System.Text.Json.JsonSerializer.Serialize(request, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
                    System.Diagnostics.Debug.WriteLine($"[EditProduct] PUT /products/{Product.IdProduct} - Request JSON (camelCase): {jsonCamel}");
                    System.Diagnostics.Debug.WriteLine($"[EditProduct] Bearer token present: {!string.IsNullOrWhiteSpace(token2)}");
                    System.Diagnostics.Debug.WriteLine($"[EditProduct] Request.IdFile: {request.IdFile}");
                    if (request.File != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EditProduct] Request.File.Filename: {request.File.Filename}");
                        System.Diagnostics.Debug.WriteLine($"[EditProduct] Request.File.FileHash: {request.File.FileHash}");
                    }
                }
                catch { }

                var response = await _api_service.UpdateProductAsync(Product.IdProduct, request, token2);

                if (response.Success)
                {
                    ToastNotification.Show("Producto actualizado", ToastType.Success, 2);
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    ToastNotification.Show(response.ErrorMessage ?? "Error al actualizar producto", ToastType.Error, 4);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[EditProduct] Save exception: {ex}");
                }
                catch { }
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[EditProduct] Save exception: {ex}");
                }
                catch { }

                ToastNotification.Show($"Error inesperado: {ex.Message}", ToastType.Error, 6);
            }
            finally
            {
                SaveButton.IsEnabled = true;
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("¿Estás seguro de eliminar este producto?", "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var token = SessionService.Instance.AuthToken ?? string.Empty;
                var result = await _apiService.DeleteProductAsync(Product.IdProduct, token);
                if (result.Success)
                {
                    ToastNotification.Show("Producto eliminado", ToastType.Success, 2);
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    ToastNotification.Show(result.ErrorMessage ?? "Error al eliminar producto", ToastType.Error, 5);
                }
            }
            catch (Exception ex)
            {
                ToastNotification.Show($"Error inesperado: {ex.Message}", ToastType.Error, 6);
            }
        }

        private async void UploadImageButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Image files|*.jpg;*.jpeg;*.png;*.gif;*.bmp|All files|*.*";
            if (dlg.ShowDialog(this) != true) return;

            var filePath = dlg.FileName;
            try
            {
                _pendingImageBytes = File.ReadAllBytes(filePath);
                _pendingImageFilename = System.IO.Path.GetFileName(filePath);
                _imageChanged = true;

                try
                {
                    if (Product.File == null)
                    {
                        Product.File = new FileData { Filename = _pendingImageFilename, FileHash = string.Empty, Type = FileType.Image, Status = FileStatus.Active };
                    }
                    else
                    {
                        Product.File.Filename = _pendingImageFilename;
                    }

                    Product.IdFile = null;
                }
                catch { }


                using var ms = new MemoryStream(_pendingImageBytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();
                PreviewImage.Source = bitmap;

                ToastNotification.Show("Imagen cargada localmente. Se subirá al guardar.", ToastType.Info, 2);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Upload error: {ex}");
                ToastNotification.Show("Error al leer la imagen", ToastType.Error, 4);
            }
        }

        private void AdjustButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PreviewImage?.Source == null)
                {
                    ToastNotification.Show("No hay imagen para ajustar", ToastType.Warning, 2);
                    return;
                }

                if (PreviewImage.Source is BitmapImage bi && bi.UriSource != null)
                {
                    var editor = new ImageEditorWindow(bi.UriSource) { Owner = this };
                    var res = editor.ShowDialog();
                    if (res == true && editor.EditedImageBytes != null)
                    {
                        _pendingImageBytes = editor.EditedImageBytes;
                        _pendingImageFilename = _pendingImageFilename ?? "edited.png";
                        _imageChanged = true;
                        using var ms = new MemoryStream(_pendingImageBytes);
                        var img = new BitmapImage();
                        img.BeginInit();
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.StreamSource = ms;
                        img.EndInit();
                        img.Freeze();
                        PreviewImage.Source = img;
                        ToastNotification.Show("Imagen ajustada (cambios locales)", ToastType.Success, 2);
                    }
                    return;
                }

                if (PreviewImage.Source is BitmapSource bsSrc)
                {
                    var editor = new ImageEditorWindow(bsSrc) { Owner = this };
                    var res = editor.ShowDialog();
                    if (res == true && editor.EditedImageBytes != null)
                    {
                        _pendingImageBytes = editor.EditedImageBytes;
                        _pendingImageFilename = _pendingImageFilename ?? "edited.png";
                        _imageChanged = true;
                        using var ms = new MemoryStream(_pendingImageBytes);
                        var img = new BitmapImage();
                        img.BeginInit();
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.StreamSource = ms;
                        img.EndInit();
                        img.Freeze();
                        PreviewImage.Source = img;
                        ToastNotification.Show("Imagen ajustada (cambios locales)", ToastType.Success, 2);
                    }
                    return;
                }

                ToastNotification.Show("Imagen no compatible para editar", ToastType.Warning, 2);
            }
            catch (Exception ex)
            {
                ToastNotification.Show($"Error al abrir editor: {ex.Message}", ToastType.Error, 4);
            }
        }

        private FileType ParseFileType(string s)
        {
            s = s?.ToLower() ?? "other";
            return s switch
            {
                "image" => FileType.Image,
                "document" => FileType.Document,
                _ => FileType.Other
            };
        }

        private async System.Threading.Tasks.Task<FileData?> CreateFileRecordAsync(FileCreateRequest req)
        {
            try
            {
                var token = SessionService.Instance.AuthToken ?? string.Empty;
                var baseUrl = AppConfiguration.Instance.ApiBaseUrl?.TrimEnd('/') ?? string.Empty;
                var url = $"{baseUrl}/files";

                using var http = new HttpClient();
                http.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
                if (!string.IsNullOrEmpty(token)) http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var json = System.Text.Json.JsonSerializer.Serialize(req, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                var resp = await http.PostAsync(url, content);
                var respText = await resp.Content.ReadAsStringAsync();
                try { System.Diagnostics.Debug.WriteLine($"[CreateFile] POST {url} - Status: {(int)resp.StatusCode} - Response: {respText}"); } catch { }

                if (!resp.IsSuccessStatusCode) return null;

                var doc = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(respText);
                var fileObj = doc.ValueKind == System.Text.Json.JsonValueKind.Array ? doc[0] : doc;

                var created = new FileData
                {
                    IdFile = fileObj.GetPropertyOrDefault(new[] { "id_file", "id" })?.GetInt32() ?? 0,
                    IdUser = fileObj.GetPropertyOrDefault(new[] { "id_user" })?.GetInt32() ?? req.IdUser,
                    Filename = fileObj.GetPropertyOrDefault(new[] { "filename" })?.GetString() ?? req.Filename,
                    FileHash = fileObj.GetPropertyOrDefault(new[] { "filehash", "fileHash", "hash" })?.GetString() ?? req.FileHash,
                    Type = ParseFileType(fileObj.GetPropertyOrDefault(new[] { "type", "service" })?.GetString() ?? "other"),
                    Status = FileStatus.Active
                };

                return created;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateFileRecordAsync error: {ex}");
                return null;
            }
        }
    }
}

