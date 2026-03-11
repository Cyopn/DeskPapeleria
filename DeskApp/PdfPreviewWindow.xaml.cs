using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Printing;
using DeskApp.Models;
using DeskApp.Services;
using Ghostscript.NET.Rasterizer;
using SkiaSharp;

namespace DeskApp
{
    public partial class PdfPreviewWindow : Window
    {
        private readonly string _pdfUrl;
        private readonly int? _transactionId;
        private readonly bool _markFilesInactiveAfterPrint;
        private readonly int? _specialServiceProductId;
        private readonly bool _markSpecialServiceInProgressAfterPrint;
        private string? _tempPdfPath;
        private GhostscriptRasterizer? _rasterizer;
        private int _pageCount;
        private int _currentPage = 1;
        private double _zoomFactor = 1.0;
        private bool _isBusy;
        private readonly ApiService _apiService;
        private readonly SessionService _sessionService;

        public PdfPreviewWindow(
            string pdfUrl,
            string? fileName,
            int? transactionId = null,
            bool markFilesInactiveAfterPrint = false,
            int? specialServiceProductId = null,
            bool markSpecialServiceInProgressAfterPrint = false)
        {
            InitializeComponent();
            _pdfUrl = pdfUrl;
            _transactionId = transactionId;
            _markFilesInactiveAfterPrint = markFilesInactiveAfterPrint;
            _specialServiceProductId = specialServiceProductId;
            _markSpecialServiceInProgressAfterPrint = markSpecialServiceInProgressAfterPrint;
            _apiService = ApiService.Instance;
            _sessionService = SessionService.Instance;
            TitleTextBlock.Text = string.IsNullOrWhiteSpace(fileName)
                ? "Vista previa de PDF"
                : $"Vista previa: {fileName}";

            SetButtonsEnabled(false);
            UpdateZoomUi();
            UpdatePdfImageWidth();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdatePdfImageWidth();
            await LoadPdfAsync();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePdfImageWidth();
        }

        private void UpdatePdfImageWidth()
        {
            var targetWidth = ActualWidth * 0.8;
            PdfPageImage.Width = Math.Max(320, targetWidth);
        }

        private async Task LoadPdfAsync()
        {
            try
            {
                SetBusy(true, "Descargando PDF...");
                _tempPdfPath = Path.Combine(Path.GetTempPath(), $"desk_preview_{Guid.NewGuid():N}.pdf");

                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
                try { http.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true"); } catch { }
                http.DefaultRequestHeaders.Accept.Clear();
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

                var token = _sessionService.AuthToken ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(token))
                {
                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                using var response = await http.GetAsync(_pdfUrl);
                response.EnsureSuccessStatusCode();

                await using (var fs = File.Create(_tempPdfPath))
                {
                    await response.Content.CopyToAsync(fs);
                }

                SetBusy(true, "Procesando PDF...");

                _rasterizer = new GhostscriptRasterizer();
                _rasterizer.Open(_tempPdfPath);
                _pageCount = _rasterizer.PageCount;
                _currentPage = 1;

                await RenderPageAsync(_currentPage);
                SetButtonsEnabled(true);
            }
            catch (Exception ex)
            {
                SetBusy(false, $"No se pudo visualizar el PDF: {ex.Message}");
                SetButtonsEnabled(false);
            }
        }

        private async Task RenderPageAsync(int pageNumber)
        {
            if (_rasterizer == null || _pageCount <= 0)
                return;

            pageNumber = Math.Clamp(pageNumber, 1, _pageCount);
            SetBusy(true, "Renderizando pagina...");

            try
            {
                var bmp = await Task.Run(() => RenderPageBitmap(pageNumber, false, 150));

                PdfPageImage.Source = bmp;
                _currentPage = pageNumber;
                PageTextBlock.Text = $"Pagina {_currentPage}/{_pageCount}";
                PageJumpTextBox.Text = _currentPage.ToString();
            }
            finally
            {
                SetBusy(false);
                RefreshPageButtons();
            }
        }

        private BitmapSource RenderPageBitmap(int pageNumber, bool grayscale, int dpi)
        {
            if (_rasterizer == null)
                throw new InvalidOperationException("PDF no cargado");

            using var pageBitmap = _rasterizer.GetPage(dpi, pageNumber);
            using var image = SKImage.FromBitmap(pageBitmap);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            using var memory = new MemoryStream(encoded.ToArray());
            memory.Position = 0;

            var imageSource = new BitmapImage();
            imageSource.BeginInit();
            imageSource.CacheOption = BitmapCacheOption.OnLoad;
            imageSource.StreamSource = memory;
            imageSource.EndInit();
            imageSource.Freeze();

            if (!grayscale)
                return imageSource;

            var gray = new FormatConvertedBitmap();
            gray.BeginInit();
            gray.Source = imageSource;
            gray.DestinationFormat = PixelFormats.Gray8;
            gray.EndInit();
            gray.Freeze();
            return gray;
        }

        private void RefreshPageButtons()
        {
            PrevPageButton.IsEnabled = !_isBusy && _currentPage > 1;
            NextPageButton.IsEnabled = !_isBusy && _currentPage < _pageCount;
            GoToPageButton.IsEnabled = !_isBusy && _pageCount > 0;
            PageJumpTextBox.IsEnabled = !_isBusy && _pageCount > 0;
            ZoomInButton.IsEnabled = !_isBusy;
            ZoomOutButton.IsEnabled = !_isBusy;
            PrintButton.IsEnabled = !_isBusy && _pageCount > 0;
        }

        private async void PrevPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                await RenderPageAsync(_currentPage - 1);
            }
        }

        private async void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _pageCount)
            {
                await RenderPageAsync(_currentPage + 1);
            }
        }

        private async void GoToPageButton_Click(object sender, RoutedEventArgs e)
        {
            await GoToTypedPageAsync();
        }

        private async void PageJumpTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await GoToTypedPageAsync();
            }
        }

        private async Task GoToTypedPageAsync()
        {
            if (_isBusy || _pageCount <= 0)
                return;

            if (!int.TryParse(PageJumpTextBox.Text?.Trim(), out var page))
            {
                PageJumpTextBox.Text = _currentPage.ToString();
                return;
            }

            page = Math.Clamp(page, 1, _pageCount);
            await RenderPageAsync(page);
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            _zoomFactor = Math.Min(3.0, _zoomFactor + 0.1);
            UpdateZoomUi();
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            _zoomFactor = Math.Max(0.5, _zoomFactor - 0.1);
            UpdateZoomUi();
        }

        private void UpdateZoomUi()
        {
            PdfScaleTransform.ScaleX = _zoomFactor;
            PdfScaleTransform.ScaleY = _zoomFactor;
            ZoomTextBlock.Text = $"{Math.Round(_zoomFactor * 100)}%";
        }

        private void SetBusy(bool busy, string? message = null)
        {
            _isBusy = busy;
            if (!string.IsNullOrWhiteSpace(message))
            {
                StatusText.Text = message;
            }

            LoadingOverlay.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            RefreshPageButtons();
        }

        private void SetButtonsEnabled(bool enabled)
        {
            if (!enabled)
            {
                PrevPageButton.IsEnabled = false;
                NextPageButton.IsEnabled = false;
                GoToPageButton.IsEnabled = false;
                PageJumpTextBox.IsEnabled = false;
                ZoomInButton.IsEnabled = false;
                ZoomOutButton.IsEnabled = false;
                PrintButton.IsEnabled = false;
                return;
            }

            RefreshPageButtons();
        }

        private async void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var token = _sessionService.AuthToken ?? string.Empty;
                var printersResult = await _apiService.GetPrintersAsync(token);
                if (!printersResult.Success || printersResult.Data == null || printersResult.Data.Count == 0)
                {
                    MessageBox.Show(printersResult.ErrorMessage ?? "No hay impresoras disponibles.", "Impresion", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var settingsWindow = new PdfPrintSettingsWindow(printersResult.Data)
                {
                    Owner = this
                };

                if (settingsWindow.ShowDialog() != true || settingsWindow.SelectedSettings == null)
                    return;

                var printed = await PrintPdfAsync(settingsWindow.SelectedSettings);
                if (printed)
                {
                    await TryMarkTransactionFilesInactiveAsync();
                    await TryMarkSpecialServiceInProgressAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo iniciar impresion: {ex.Message}", "Impresion", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool MarkedFilesInactive { get; private set; }

        private async Task TryMarkTransactionFilesInactiveAsync()
        {
            if (!_markFilesInactiveAfterPrint || !_transactionId.HasValue)
                return;

            try
            {
                var token = _sessionService.AuthToken ?? string.Empty;
                var update = await _apiService.UpdateTransactionFilesStatusAsync(_transactionId.Value, "inactive", token);
                if (!update.Success)
                {
                    MessageBox.Show(update.ErrorMessage ?? "La impresión fue enviada, pero no se pudo actualizar el estado de archivos.", "Impresion", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MarkedFilesInactive = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"La impresión fue enviada, pero falló la actualización de estado: {ex.Message}", "Impresion", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task TryMarkSpecialServiceInProgressAsync()
        {
            if (!_markSpecialServiceInProgressAfterPrint || !_specialServiceProductId.HasValue)
                return;

            try
            {
                var token = _sessionService.AuthToken ?? string.Empty;
                var update = await _apiService.UpdateProductSpecialServiceStatusAsync(_specialServiceProductId.Value, "in_progress", token);
                if (!update.Success)
                {
                    MessageBox.Show(update.ErrorMessage ?? "La impresión fue enviada, pero no se pudo actualizar el estado del servicio especial.", "Impresion", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"La impresión fue enviada, pero falló la actualización del servicio especial: {ex.Message}", "Impresion", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task<bool> PrintPdfAsync(PdfPrintSettings settings)
        {
            if (_rasterizer == null || _pageCount <= 0)
            {
                MessageBox.Show("No hay PDF cargado para imprimir.", "Impresion", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var pages = ParsePageRange(settings.Range, _pageCount);
            if (pages.Count == 0)
            {
                MessageBox.Show("Rango de paginas invalido.", "Impresion", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            SetBusy(true, "Preparando impresion...");

            try
            {
                var printQueue = ResolvePrintQueue(settings.PrinterName);
                if (printQueue == null)
                {
                    MessageBox.Show("La impresora seleccionada no existe en Windows.", "Impresion", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                var printDialog = new PrintDialog
                {
                    PrintQueue = printQueue,
                    PrintTicket = printQueue.DefaultPrintTicket
                };

                printDialog.PrintTicket.CopyCount = 1;
                printDialog.PrintTicket.Duplexing = settings.BothSides
                    ? Duplexing.TwoSidedLongEdge
                    : Duplexing.OneSided;
                printDialog.PrintTicket.OutputColor = settings.ColorMode == "bw"
                    ? OutputColor.Monochrome
                    : OutputColor.Color;

                var capabilities = printQueue.GetPrintCapabilities(printDialog.PrintTicket);
                var printableWidth = capabilities.PageImageableArea?.ExtentWidth ?? 793;
                var printableHeight = capabilities.PageImageableArea?.ExtentHeight ?? 1122;

                var document = BuildPrintDocument(settings, pages, printableWidth, printableHeight);
                printDialog.PrintDocument(document.DocumentPaginator, "DeskApp PDF");

                MessageBox.Show("Impresion enviada correctamente.", "Impresion", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir: {ex.Message}", "Impresion", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally
            {
                SetBusy(false);
            }
        }

        public async Task<bool> PrintWithSettingsAsync(PdfPrintSettings settings)
        {
            try
            {
                if (_rasterizer == null || _pageCount <= 0)
                {
                    await LoadPdfAsync();
                }

                if (_rasterizer == null || _pageCount <= 0)
                {
                    return false;
                }

                return await PrintPdfAsync(settings);
            }
            catch
            {
                return false;
            }
        }

        private FixedDocument BuildPrintDocument(PdfPrintSettings settings, List<int> pages, double printableWidth, double printableHeight)
        {
            var document = new FixedDocument();
            var grayscale = settings.ColorMode == "bw";
            var totalDocs = settings.Copies * settings.Sets;

            for (var docIndex = 0; docIndex < totalDocs; docIndex++)
            {
                foreach (var pageNumber in pages)
                {
                    var bitmap = RenderPageBitmap(pageNumber, grayscale, 220);
                    var page = new FixedPage
                    {
                        Width = printableWidth,
                        Height = printableHeight
                    };

                    var image = new Image
                    {
                        Source = bitmap,
                        Width = printableWidth,
                        Height = printableHeight,
                        Stretch = Stretch.Uniform
                    };

                    FixedPage.SetLeft(image, 0);
                    FixedPage.SetTop(image, 0);
                    page.Children.Add(image);

                    var pageContent = new PageContent();
                    ((System.Windows.Markup.IAddChild)pageContent).AddChild(page);
                    document.Pages.Add(pageContent);
                }
            }

            return document;
        }

        private static PrintQueue? ResolvePrintQueue(string printerName)
        {
            try
            {
                var server = new LocalPrintServer();
                var queues = server.GetPrintQueues();
                return queues.FirstOrDefault(q => string.Equals(q.Name, printerName, StringComparison.OrdinalIgnoreCase))
                       ?? queues.FirstOrDefault(q => q.Name.Contains(printerName, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return null;
            }
        }

        private static List<int> ParsePageRange(string? range, int maxPage)
        {
            if (string.IsNullOrWhiteSpace(range) || string.Equals(range.Trim(), "all", StringComparison.OrdinalIgnoreCase))
            {
                return Enumerable.Range(1, maxPage).ToList();
            }

            var pages = new SortedSet<int>();
            var parts = range.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var part in parts)
            {
                if (part.Contains('-'))
                {
                    var pair = part.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (pair.Length != 2 || !int.TryParse(pair[0], out var start) || !int.TryParse(pair[1], out var end))
                        continue;

                    if (end < start)
                    {
                        (start, end) = (end, start);
                    }

                    for (var i = start; i <= end; i++)
                    {
                        if (i >= 1 && i <= maxPage)
                        {
                            pages.Add(i);
                        }
                    }
                }
                else if (int.TryParse(part, out var page) && page >= 1 && page <= maxPage)
                {
                    pages.Add(page);
                }
            }

            return pages.ToList();
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                _rasterizer?.Dispose();
                _rasterizer = null;
            }
            catch { }

            try
            {
                if (!string.IsNullOrWhiteSpace(_tempPdfPath) && File.Exists(_tempPdfPath))
                {
                    File.Delete(_tempPdfPath);
                }
            }
            catch { }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
