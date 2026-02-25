using System;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using DeskApp.Models;
using DeskApp.Services;
using DeskApp.Configuration;
using DeskApp.Extensions;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace DeskApp
{
    public partial class CreateProductWindow : Window
    {
        private readonly ApiService _apiService;
        private FileData? _uploadedFile;
        private byte[]? _pendingImageBytes;
        private string? _pendingImageFilename;

        public CreateProductWindow()
        {
            InitializeComponent();
            _apiService = ApiService.Instance;
        }

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Image files|*.jpg;*.jpeg;*.png;*.gif;*.bmp|All files|*.*";
            if (dlg.ShowDialog(this) != true) return;

            var filePath = dlg.FileName;

            try
            {
                _pendingImageBytes = File.ReadAllBytes(filePath);
                _pendingImageFilename = System.IO.Path.GetFileName(filePath);
                FileNameTextBox.Text = _pendingImageFilename;

                try
                {
                    if (_uploadedFile == null)
                    {
                        _uploadedFile = new FileData { Filename = _pendingImageFilename, FileHash = string.Empty, Type = FileType.Image, Status = FileStatus.Active };
                    }
                    else
                    {
                        _uploadedFile.Filename = _pendingImageFilename;
                    }
                }
                catch { }

                using var ms = new MemoryStream(_pendingImageBytes);
                var img = new System.Windows.Media.Imaging.BitmapImage();
                img.BeginInit();
                img.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                img.StreamSource = ms;
                img.EndInit();
                img.Freeze();
                PreviewImage.Source = img;

                ToastNotification.Show("Imagen cargada localmente. Se subirá al crear.", ToastType.Info, 2);
            }
            catch (Exception ex)
            {
                ToastNotification.Show($"Error al leer imagen: {ex.Message}", ToastType.Error, 4);
            }
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
                if (!resp.IsSuccessStatusCode) return null;

                var doc = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(respText);
                var fileObj = doc.ValueKind == System.Text.Json.JsonValueKind.Array ? doc[0] : doc;

                var idEl = fileObj.GetPropertyOrDefault(new[] { "id_file", "id" });
                var id = idEl.HasValue ? idEl.Value.GetInt32() : 0;

                var userEl = fileObj.GetPropertyOrDefault(new[] { "id_user" });
                var idUser = userEl.HasValue ? userEl.Value.GetInt32() : req.IdUser;

                var filenameEl = fileObj.GetPropertyOrDefault(new[] { "filename" });
                var filename = filenameEl.HasValue ? filenameEl.Value.GetString() : req.Filename;

                var hashEl = fileObj.GetPropertyOrDefault(new[] { "filehash", "fileHash", "hash" });
                var filehash = hashEl.HasValue ? hashEl.Value.GetString() : req.FileHash;

                var created = new FileData
                {
                    IdFile = id,
                    IdUser = idUser,
                    Filename = filename,
                    FileHash = filehash,
                    Type = FileType.Image,
                    Status = FileStatus.Active
                };

                return created;
            }
            catch
            {
                return null;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
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
                        using var ms = new MemoryStream(editor.EditedImageBytes);
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
                        using var ms = new MemoryStream(editor.EditedImageBytes);
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

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!decimal.TryParse(PriceTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
                {
                    ToastNotification.Show("Precio no válido", ToastType.Warning, 3);
                    return;
                }

                if (_pendingImageBytes != null && (_uploadedFile == null || _uploadedFile.IdFile == 0))
                {
                    try
                    {
                        var baseUrl = AppConfiguration.Instance.ApiBaseUrl?.TrimEnd('/') ?? string.Empty;
                        var url = $"{baseUrl}/file-manager?service=image";

                        using var http = new HttpClient();
                        http.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
                        var uploadToken = SessionService.Instance.AuthToken;
                        if (!string.IsNullOrEmpty(uploadToken)) http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", uploadToken);

                        using var fd = new MultipartFormDataContent();
                        using var ms = new MemoryStream(_pendingImageBytes);
                        var fileName = _pendingImageFilename ?? "image.png";
                        var streamContent = new StreamContent(ms);
                        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                        fd.Add(streamContent, "files", fileName);
                        fd.Add(new StringContent(SessionService.Instance.CurrentUser?.Username ?? ""), "username");

                        var uploadResp = await http.PostAsync(url, fd);
                        var uploadRespText = await uploadResp.Content.ReadAsStringAsync();
                        if (!uploadResp.IsSuccessStatusCode)
                        {
                            ToastNotification.Show("Error al subir imagen al servidor", ToastType.Error, 4);
                            return;
                        }

                        var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(uploadRespText);
                        var fileInfo = json.ValueKind == System.Text.Json.JsonValueKind.Array ? json[0] : json;

                        var originalNameEl = fileInfo.GetPropertyOrDefault(new[] { "originalName", "originalname", "filename" });
                        var originalName = originalNameEl.HasValue ? originalNameEl.Value.GetString() : fileName;

                        var storedNameEl = fileInfo.GetPropertyOrDefault(new[] { "storedName", "storedname", "filename", "stored_file", "hash" });
                        var storedName = storedNameEl.HasValue ? storedNameEl.Value.GetString() : string.Empty;

                        var fileCreateReq = new FileCreateRequest
                        {
                            IdUser = SessionService.Instance.CurrentUser?.IdUser ?? 1,
                            Filename = originalName ?? fileName,
                            Type = FileType.Image,
                            FileHash = storedName ?? string.Empty
                        };

                        var created = await CreateFileRecordAsync(fileCreateReq);
                        if (created == null)
                        {
                            ToastNotification.Show("Error al crear registro de archivo", ToastType.Error, 4);
                            return;
                        }

                        _uploadedFile = created;
                        FileNameTextBox.Text = created.Filename;

                    }
                    catch (Exception ex)
                    {
                        ToastNotification.Show($"Error al subir imagen: {ex.Message}", ToastType.Error, 4);
                        return;
                    }
                }
                if (_uploadedFile != null && _uploadedFile.IdFile == 0)
                {
                    _uploadedFile = null;
                }

                var itemCategory = ItemCategory.Otros;
                var categoryCombo = this.FindName("CategoryComboBox") as ComboBox;
                if (categoryCombo != null)
                {
                    var sel = categoryCombo.SelectedItem;

                    if (sel is ItemCategory ic)
                    {
                        itemCategory = ic;
                    }
                    else if (sel is ComboBoxItem cbi)
                    {
                        if (cbi.Tag is ItemCategory tagCat)
                        {
                            itemCategory = tagCat;
                        }
                        else if (cbi.Tag is string tagStr && Enum.TryParse<ItemCategory>(tagStr, true, out var parsedTag))
                        {
                            itemCategory = parsedTag;
                        }
                        else if (cbi.Content != null && Enum.TryParse<ItemCategory>(cbi.Content.ToString(), true, out var parsedContent))
                        {
                            itemCategory = parsedContent;
                        }
                    }
                    else if (sel is string s)
                    {
                        if (Enum.TryParse<ItemCategory>(s, true, out var parsed)) itemCategory = parsed;
                    }
                    else
                    {
                        var txt = categoryCombo.Text ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(txt) && Enum.TryParse<ItemCategory>(txt, true, out var parsedText))
                        {
                            itemCategory = parsedText;
                        }
                    }

                    if (itemCategory == default(ItemCategory)) itemCategory = ItemCategory.Otros;
                }
                else
                {
                    itemCategory = ItemCategory.Otros;
                }

                var itemNameBox = this.FindName("ItemNameTextBox") as TextBox;
                var productDescBox = this.FindName("ProductDescriptionTextBox") as TextBox;

                var req = new ProductCreateRequest
                {
                    Type = DeskApp.ProductTypeEnum.Item,
                    Name = itemNameBox?.Text ?? string.Empty,
                    Description = productDescBox?.Text ?? string.Empty,
                    Price = price,
                    IdFile = _uploadedFile?.IdFile,
                    Category = itemCategory,
                    Item = new ItemCreateRequest
                    {
                        Name = itemNameBox?.Text ?? string.Empty,
                        Available = AvailableCheckBox.IsChecked == true,
                        Category = itemCategory
                    }
                };

                var token = SessionService.Instance.AuthToken ?? string.Empty;
                var resp = await _apiService.CreateProductAsync(req, token);
                if (resp.Success)
                {
                    _pendingImageBytes = null;
                    _pendingImageFilename = null;
                    ToastNotification.Show("Producto creado", ToastType.Success, 2);
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    ToastNotification.Show(resp.ErrorMessage ?? "Error al crear producto", ToastType.Error, 4);
                }
            }
            catch (Exception ex)
            {
                ToastNotification.Show($"Error inesperado: {ex.Message}", ToastType.Error, 4);
            }
        }
    }
}
