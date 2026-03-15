using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DeskApp.Models;
using DeskApp.Services;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace DeskApp
{
    public partial class QrScanWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly SessionService _sessionService;
        private static readonly HttpClient _httpClient = new() { Timeout = Timeout.InfiniteTimeSpan };
        private readonly string _cameraConfigPath;
        private CancellationTokenSource? _readCts;
        private bool _isReading;

        public TransactionData? ScannedTransaction { get; private set; }

        public QrScanWindow()
        {
            InitializeComponent();
            _apiService = ApiService.Instance;
            _sessionService = SessionService.Instance;

            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeskApp");
            Directory.CreateDirectory(appData);
            _cameraConfigPath = Path.Combine(appData, "camera_ip.txt");

            LoadCameraIp();
        }

        private void LoadCameraIp()
        {
            try
            {
                if (File.Exists(_cameraConfigPath))
                {
                    CameraIpTextBox.Text = File.ReadAllText(_cameraConfigPath).Trim();
                }
            }
            catch
            {
            }
        }

        private void SaveCameraIpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                File.WriteAllText(_cameraConfigPath, CameraIpTextBox.Text?.Trim() ?? string.Empty);
                MessageBox.Show("IP de camara guardada localmente.", "QR", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo guardar la IP: {ex.Message}", "QR", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private Button? ReadButton => FindName("ReadFromCameraButton") as Button;
        private Image? PreviewImage => FindName("CameraPreviewImage") as Image;
        private TextBlock? StatusBlock => FindName("StatusTextBlock") as TextBlock;

        private async void ReadFromCameraButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isReading)
            {
                StopRealtimeRead();
                return;
            }

            var cameraUrl = CameraIpTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cameraUrl))
            {
                MessageBox.Show("Ingresa la URL/IP de la camara.", "QR", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _readCts = new CancellationTokenSource();
            _isReading = true;
            if (ReadButton != null) ReadButton.Content = "Detener";
            if (StatusBlock != null) StatusBlock.Text = "Leyendo en tiempo real...";

            try
            {
                await ReadRealtimeLoopAsync(cameraUrl, _readCts.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (StatusBlock != null) StatusBlock.Text = $"Error de lectura: {ex.Message}";
            }
            finally
            {
                _isReading = false;
                if (ReadButton != null) ReadButton.Content = "Iniciar lectura";
                _readCts?.Dispose();
                _readCts = null;
            }
        }

        private async Task ReadRealtimeLoopAsync(string cameraUrl, CancellationToken cancellationToken)
        {
            var reader = new BarcodeReaderGeneric
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE }
                }
            };

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, cameraUrl);
                    using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        if (StatusBlock != null) StatusBlock.Text = $"Camara no disponible (HTTP {(int)response.StatusCode})";
                        await Task.Delay(800, cancellationToken);
                        continue;
                    }

                    var mediaType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? string.Empty;

                    if (mediaType.Contains("multipart") || mediaType.Contains("mjpeg"))
                    {
                        if (StatusBlock != null) StatusBlock.Text = "Recibiendo video en tiempo real...";
                        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                        var scanned = await ReadMjpegStreamAsync(stream, reader, cancellationToken);
                        if (scanned)
                            return;
                    }
                    else
                    {
                        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                        var scanned = await ProcessFrameAsync(bytes, reader);
                        if (scanned)
                            return;

                        await Task.Delay(180, cancellationToken);
                    }
                }
                catch (TaskCanceledException)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    await Task.Delay(500, cancellationToken);
                }
                catch
                {
                    await Task.Delay(500, cancellationToken);
                }
            }
        }

        private async Task<bool> ReadMjpegStreamAsync(Stream stream, BarcodeReaderGeneric reader, CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];
            using var frameBuffer = new MemoryStream();
            var capturing = false;
            var prev = -1;

            while (!cancellationToken.IsCancellationRequested)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read <= 0)
                    return false;

                for (var i = 0; i < read; i++)
                {
                    var current = buffer[i];

                    if (!capturing)
                    {
                        if (prev == 0xFF && current == 0xD8)
                        {
                            capturing = true;
                            frameBuffer.SetLength(0);
                            frameBuffer.WriteByte(0xFF);
                            frameBuffer.WriteByte(0xD8);
                        }
                    }
                    else
                    {
                        frameBuffer.WriteByte(current);

                        if (prev == 0xFF && current == 0xD9)
                        {
                            var frame = frameBuffer.ToArray();
                            var scanned = await ProcessFrameAsync(frame, reader);
                            if (scanned)
                                return true;

                            capturing = false;
                            frameBuffer.SetLength(0);
                        }
                    }

                    prev = current;
                }
            }

            return false;
        }

        private async Task<bool> ProcessFrameAsync(byte[] bytes, BarcodeReaderGeneric reader)
        {
            if (bytes == null || bytes.Length == 0)
                return false;

            UpdatePreview(bytes);

            string? qrText = null;
            try
            {
                using var ms = new MemoryStream(bytes);
                using var bitmap = (System.Drawing.Bitmap)System.Drawing.Image.FromStream(ms);
                var luminance = new BitmapLuminanceSource(bitmap);
                var qrResult = reader.Decode(luminance);
                qrText = qrResult?.Text;
            }
            catch
            {
            }

            if (string.IsNullOrWhiteSpace(qrText))
                return false;

            var json = ExtractJsonObject(qrText);
            if (string.IsNullOrWhiteSpace(json))
                return false;

            QrJsonTextBox.Text = json;
            if (StatusBlock != null) StatusBlock.Text = "QR detectado. Buscando transaccion...";
            return await TryScanQrAsync(json);
        }

        private void UpdatePreview(byte[] bytes)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    using var ms = new MemoryStream(bytes);
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                    image.Freeze();
                    if (PreviewImage != null)
                    {
                        PreviewImage.Source = image;
                    }
                }
                catch
                {
                }
            });
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            var rawJson = QrJsonTextBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                MessageBox.Show("Pega o carga el JSON del QR.", "QR", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ok = await TryScanQrAsync(rawJson);
            if (!ok)
            {
                MessageBox.Show("No se pudo escanear el QR.", "QR", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task<bool> TryScanQrAsync(string rawJson)
        {
            try
            {
                using var _ = JsonDocument.Parse(rawJson);
            }
            catch
            {
                MessageBox.Show("El JSON del QR no es valido.", "QR", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            try
            {
                var token = _sessionService.Token ?? string.Empty;
                var result = await _apiService.ScanTransactionQrAsync(rawJson, token);

                if (result.Success && result.Data?.Transaction != null)
                {
                    ScannedTransaction = result.Data.Transaction;
                    StopRealtimeRead();
                    DialogResult = true;
                    Close();
                    return true;
                }

                if (StatusBlock != null) StatusBlock.Text = result.ErrorMessage ?? "No se pudo escanear el QR.";
                return false;
            }
            catch (Exception ex)
            {
                if (StatusBlock != null) StatusBlock.Text = $"Error al escanear QR: {ex.Message}";
                return false;
            }
        }

        private static string ExtractJsonObject(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var trimmed = input.Trim();
            if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
                return trimmed;

            var match = Regex.Match(input, "\\{.*\\}", RegexOptions.Singleline);
            return match.Success ? match.Value : string.Empty;
        }

        private void StopRealtimeRead()
        {
            try
            {
                _readCts?.Cancel();
            }
            catch
            {
            }
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            StopRealtimeRead();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            StopRealtimeRead();
            DialogResult = false;
            Close();
        }
    }
}
