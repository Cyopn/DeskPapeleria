using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using DeskApp.Configuration;
using DeskApp.Models;
using DeskApp.Services;

namespace DeskApp
{
    public partial class TransactionDetailsWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly List<FileData> _transactionFiles = new();

        public TransactionData Transaction { get; }
        public ObservableCollection<TransactionDetailRow> Items { get; } = new();

        public string HeaderLine => $"Transacción #{Transaction.IdTransaction} · {Transaction.Type} · {Transaction.Total:C}";
        public string MetaLine => $"Usuario: {GetUserDisplay()} · Estado: {Transaction.Status} · Pago: {GetPaymentMethodDisplay(Transaction.PaymentMethod)}";
        public string FooterLine => $"Fecha: {Transaction.Date:g}";

        public TransactionDetailsWindow(TransactionData transaction)
        {
            InitializeComponent();
            _apiService = ApiService.Instance;
            _sessionService = SessionService.Instance;
            Transaction = transaction;
            DataContext = this;

            _transactionFiles.AddRange(GetTransactionFiles());
            UpdateActionButtons();

            LoadRows();
            LoadQrImage();
        }

        private void UpdateActionButtons()
        {
            var hasActiveFiles = _transactionFiles.Any(f => f.Status == FileStatus.Active);
            var hasPendingSpecialServices = GetPendingSpecialServiceProductIds().Count > 0;
            var hasActiveSpecialServiceFiles = HasActiveFilesForProductType(IsSpecialServiceProduct);
            var hasActivePrintFiles = HasActiveFilesForProductType(IsPrintProduct);

            PrintFilesButton.IsEnabled = hasActiveFiles;
            CompleteSpecialServiceButton.IsEnabled = hasPendingSpecialServices && !hasActiveSpecialServiceFiles;
            CompletePurchaseButton.IsEnabled = Transaction.Status != TransactionStatusEnum.Completed
                                             && !hasPendingSpecialServices
                                             && !hasActivePrintFiles
                                             && !hasActiveSpecialServiceFiles;
        }

        private bool HasActiveFilesForProductType(Func<ProductData?, bool> productPredicate)
        {
            if (Transaction.Details == null)
                return false;

            foreach (var detail in Transaction.Details)
            {
                var product = detail.Product;
                if (!productPredicate(product))
                    continue;

                if (HasActiveAssociatedFile(product))
                    return true;
            }

            return false;
        }

        private bool HasActiveAssociatedFile(ProductData? product)
        {
            if (product == null)
                return false;

            if (GetAssociatedFiles(product).Any(f => f.Status == FileStatus.Active))
                return true;

            var associatedIds = GetAssociatedFileIds(product);
            if (associatedIds.Count > 0 && _transactionFiles.Any(f =>
                    f.Status == FileStatus.Active &&
                    (associatedIds.Contains(f.IdFile) || associatedIds.Contains(f.Id))))
            {
                return true;
            }

            var linkedPrintProduct = ResolveLinkedPrintProduct(product);
            if (linkedPrintProduct != null)
            {
                if (GetAssociatedFiles(linkedPrintProduct).Any(f => f.Status == FileStatus.Active))
                    return true;

                var linkedIds = GetAssociatedFileIds(linkedPrintProduct);
                if (linkedIds.Count > 0 && _transactionFiles.Any(f =>
                        f.Status == FileStatus.Active &&
                        (linkedIds.Contains(f.IdFile) || linkedIds.Contains(f.Id))))
                {
                    return true;
                }
            }

            return false;
        }

        private ProductData? ResolveLinkedPrintProduct(ProductData product)
        {
            if (Transaction.Details == null)
                return null;

            var printId = product.SpecialService?.Data?.IdPrint;
            if (printId is null or <= 0)
                printId = product.SpecialService?.IdPrint > 0 ? product.SpecialService.IdPrint : null;

            if (printId is null or <= 0)
                return null;

            return Transaction.Details
                .Select(d => d.Product)
                .FirstOrDefault(p => p?.Print != null && p.Print.IdPrint == printId.Value);
        }

        private static HashSet<int> GetAssociatedFileIds(ProductData product)
        {
            var ids = new HashSet<int>();

            if (product.IdFile.HasValue)
                ids.Add(product.IdFile.Value);

            if (product.IdFiles != null)
            {
                foreach (var id in product.IdFiles)
                {
                    ids.Add(id);
                }
            }

            var linkedPrintProduct = product.SpecialService?.Print?.Product;
            if (linkedPrintProduct != null)
            {
                if (linkedPrintProduct.IdFile.HasValue)
                    ids.Add(linkedPrintProduct.IdFile.Value);

                if (linkedPrintProduct.IdFiles != null)
                {
                    foreach (var id in linkedPrintProduct.IdFiles)
                    {
                        ids.Add(id);
                    }
                }
            }

            return ids;
        }

        private static IEnumerable<FileData> GetAssociatedFiles(ProductData product)
        {
            var files = new List<FileData>();

            if (product.File != null)
            {
                files.Add(product.File);
            }

            if (product.Files != null)
            {
                files.AddRange(product.Files);
            }

            var linkedPrintProduct = product.SpecialService?.Print?.Product;
            if (linkedPrintProduct != null)
            {
                if (linkedPrintProduct.File != null)
                {
                    files.Add(linkedPrintProduct.File);
                }

                if (linkedPrintProduct.Files != null)
                {
                    files.AddRange(linkedPrintProduct.Files);
                }
            }

            return files
                .Where(f => !string.IsNullOrWhiteSpace(f.FileHash))
                .GroupBy(f => f.FileHash!, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First());
        }

        private static bool IsPrintProduct(ProductData? product)
        {
            return product != null && (product.Type == ProductTypeEnum.Print || product.Print != null);
        }

        private List<int> GetPendingSpecialServiceProductIds()
        {
            if (Transaction.Details == null)
                return new List<int>();

            return Transaction.Details
                .Where(d => IsSpecialServiceProduct(d.Product) && !IsSpecialServiceCompleted(d.Product))
                .Select(d => d.IdProduct)
                .Distinct()
                .ToList();
        }

        private static bool IsSpecialServiceProduct(ProductData? product)
        {
            return product != null && (product.Type == ProductTypeEnum.SpecialService || product.SpecialService != null);
        }

        private static bool IsSpecialServiceCompleted(ProductData? product)
        {
            var status = product?.SpecialService?.Status
                         ?? product?.SpecialService?.SpecialService?.Status;

            return string.Equals(status?.Trim(), "completed", StringComparison.OrdinalIgnoreCase);
        }

        private void LoadRows()
        {
            Items.Clear();

            if (Transaction.Details == null)
                return;

            foreach (var d in Transaction.Details)
            {
                var name = d.Product?.Item?.Name
                           ?? d.Product?.Description
                           ?? $"Producto #{d.IdProduct}";

                var type = d.Product?.Type.ToString() ?? "No aplica";
                var previewFile = GetPreviewFile(d.Product);
                var showPreviewButton = ShouldShowPdfPreviewButton(d.Product, previewFile);

                Items.Add(new TransactionDetailRow
                {
                    DetailId = d.IdDetailTransaction,
                    ProductId = d.IdProduct,
                    IsSpecialService = IsSpecialServiceProduct(d.Product),
                    ProductName = name,
                    ProductType = type,
                    Amount = d.Amount,
                    Price = d.Price,
                    Subtotal = d.Price * d.Amount,
                    PreviewFile = previewFile,
                    IsPreviewAvailable = showPreviewButton,
                    ShowPreviewButton = showPreviewButton
                });
            }
        }

        private static bool ShouldShowPdfPreviewButton(ProductData? product, FileData? previewFile)
        {
            if (product == null || previewFile == null)
                return false;

            if (product.Type == ProductTypeEnum.Item || product.Item != null)
                return false;

            if (previewFile.Type == FileType.Image)
                return false;

            var filename = previewFile.Filename?.Trim().ToLowerInvariant() ?? string.Empty;
            if (filename.EndsWith(".png") || filename.EndsWith(".jpg") || filename.EndsWith(".jpeg") || filename.EndsWith(".bmp") || filename.EndsWith(".gif") || filename.EndsWith(".webp"))
                return false;

            return true;
        }

        private static FileData? GetPreviewFile(ProductData? product)
        {
            if (product == null)
                return null;

            if (product.File != null && !string.IsNullOrWhiteSpace(product.File.FileHash))
                return product.File;

            return product.Files?.LastOrDefault(f => !string.IsNullOrWhiteSpace(f.FileHash));
        }

        private List<FileData> GetTransactionFiles()
        {
            var files = new List<FileData>();

            if (Transaction.Details == null)
                return files;

            foreach (var detail in Transaction.Details)
            {
                var product = detail.Product;
                if (product == null)
                    continue;

                if (product.File != null && !string.IsNullOrWhiteSpace(product.File.FileHash))
                {
                    files.Add(product.File);
                }

                if (product.Files != null)
                {
                    files.AddRange(product.Files.Where(f => !string.IsNullOrWhiteSpace(f.FileHash)));
                }
            }

            return files
                .GroupBy(f => !string.IsNullOrWhiteSpace(f.FileHash) ? f.FileHash : $"id:{f.IdFile}")
                .Select(g => g.First())
                .ToList();
        }

        private async void PrintFilesButton_Click(object sender, RoutedEventArgs e)
        {
            var activeFiles = _transactionFiles
                .Where(f => f.Status == FileStatus.Active && !string.IsNullOrWhiteSpace(f.FileHash))
                .ToList();

            if (activeFiles.Count == 0)
            {
                ToastNotification.Show("No hay archivos activos para imprimir", ToastType.Warning, 3);
                return;
            }

            var printableFiles = activeFiles.Where(IsPrintablePdfFile).ToList();
            if (printableFiles.Count == 0)
            {
                ToastNotification.Show("No hay archivos PDF activos para imprimir", ToastType.Warning, 3);
                return;
            }

            try
            {
                var token = _sessionService.AuthToken ?? string.Empty;
                var printersResult = await _apiService.GetPrintersAsync(token);
                if (!printersResult.Success || printersResult.Data == null || printersResult.Data.Count == 0)
                {
                    ToastNotification.Show(printersResult.ErrorMessage ?? "No hay impresoras disponibles", ToastType.Warning, 4);
                    return;
                }

                var settingsWindow = new PdfPrintSettingsWindow(printersResult.Data)
                {
                    Owner = this
                };

                if (settingsWindow.ShowDialog() != true || settingsWindow.SelectedSettings == null)
                    return;

                var allPrinted = true;
                var printedCount = 0;
                var printedFiles = new List<FileData>();
                var printedSpecialServiceProductIds = new HashSet<int>();
                foreach (var file in printableFiles)
                {
                    var url = BuildFileDownloadUrl(file);
                    if (string.IsNullOrWhiteSpace(url))
                    {
                        allPrinted = false;
                        continue;
                    }

                    var previewWindow = new PdfPreviewWindow(url, file.Filename)
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        ShowInTaskbar = false
                    };

                    var printed = await previewWindow.PrintWithSettingsAsync(settingsWindow.SelectedSettings);
                    previewWindow.Close();

                    if (printed)
                    {
                        printedCount++;
                        printedFiles.Add(file);

                        var specialProductId = FindSpecialServiceProductIdByFile(file);
                        if (specialProductId.HasValue)
                        {
                            printedSpecialServiceProductIds.Add(specialProductId.Value);
                        }
                    }
                    else
                    {
                        allPrinted = false;
                    }
                }

                if (printedCount == 0)
                {
                    ToastNotification.Show("No se pudo enviar ningún archivo a impresión", ToastType.Error, 4);
                    return;
                }

                if (!allPrinted)
                {
                    ToastNotification.Show($"Se imprimieron {printedCount} de {printableFiles.Count} archivo(s)", ToastType.Warning, 4);
                    return;
                }

                foreach (var productId in printedSpecialServiceProductIds)
                {
                    var specialResult = await _apiService.UpdateProductSpecialServiceStatusAsync(productId, "in_progress", token);
                    if (!specialResult.Success)
                    {
                        ToastNotification.Show(specialResult.ErrorMessage ?? $"No se pudo actualizar servicio especial del producto {productId}", ToastType.Warning, 4);
                    }
                    else
                    {
                        SetSpecialServiceStatus(productId, "in_progress");
                    }
                }

                var filePatchErrors = 0;
                foreach (var printedFile in printedFiles)
                {
                    var updateResult = await _apiService.UpdateTransactionFilesStatusAsync(Transaction.IdTransaction, "inactive", token);
                    if (!updateResult.Success)
                    {
                        filePatchErrors++;
                        ToastNotification.Show(updateResult.ErrorMessage ?? $"No se pudo actualizar estado del archivo {printedFile.Filename}", ToastType.Warning, 4);
                        continue;
                    }

                    printedFile.Status = FileStatus.Inactive;
                    foreach (var file in _transactionFiles.Where(f =>
                                 (!string.IsNullOrWhiteSpace(f.FileHash) && string.Equals(f.FileHash, printedFile.FileHash, StringComparison.OrdinalIgnoreCase))
                                 || (f.IdFile > 0 && f.IdFile == printedFile.IdFile)
                                 || (f.Id > 0 && f.Id == printedFile.Id)))
                    {
                        file.Status = FileStatus.Inactive;
                    }
                }

                UpdateActionButtons();
                if (filePatchErrors == 0)
                {
                    ToastNotification.Show("Impresión completada y archivos marcados como inactivos", ToastType.Success, 4);
                }
                else
                {
                    ToastNotification.Show("Impresión completada, pero hubo errores al actualizar algunos archivos", ToastType.Warning, 4);
                }
            }
            catch (Exception ex)
            {
                ToastNotification.Show($"Error al imprimir archivos: {ex.Message}", ToastType.Error, 4);
            }
        }

        private static bool IsPrintablePdfFile(FileData file)
        {
            if (file.Type == FileType.Image)
                return false;

            var filename = file.Filename?.Trim().ToLowerInvariant() ?? string.Empty;
            if (filename.EndsWith(".png") || filename.EndsWith(".jpg") || filename.EndsWith(".jpeg") || filename.EndsWith(".bmp") || filename.EndsWith(".gif") || filename.EndsWith(".webp"))
                return false;

            return true;
        }

        private static string BuildFileDownloadUrl(FileData file)
        {
            var baseUrl = AppConfiguration.Instance.ApiBaseUrl?.TrimEnd('/') ?? string.Empty;
            var fileHash = file.FileHash ?? string.Empty;
            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(fileHash))
                return string.Empty;

            var typeStr = file.Type.ToString().ToLowerInvariant();
            return $"{baseUrl}/file-manager/download/{typeStr}/{fileHash}";
        }

        private void PreviewFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: TransactionDetailRow row } || row.PreviewFile == null)
                return;

            try
            {
                var baseUrl = AppConfiguration.Instance.ApiBaseUrl?.TrimEnd('/') ?? string.Empty;
                var typeStr = row.PreviewFile.Type.ToString().ToLowerInvariant();
                var fileHash = row.PreviewFile.FileHash ?? string.Empty;

                if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(fileHash))
                {
                    ToastNotification.Show("No se pudo generar la URL del archivo", ToastType.Warning, 3);
                    return;
                }

                var url = $"{baseUrl}/file-manager/download/{typeStr}/{fileHash}";
                var previewWindow = new PdfPreviewWindow(url, row.PreviewFile.Filename, Transaction.IdTransaction, true, row.IsSpecialService ? row.ProductId : null, row.IsSpecialService)
                {
                    Owner = this
                };
                previewWindow.ShowDialog();

                if (previewWindow.MarkedFilesInactive)
                {
                    foreach (var file in _transactionFiles.Where(f => f.Status == FileStatus.Active))
                    {
                        file.Status = FileStatus.Inactive;
                    }

                    if (row.IsSpecialService)
                    {
                        SetSpecialServiceStatus(row.ProductId, "in_progress");
                    }

                    UpdateActionButtons();
                }
            }
            catch (Exception ex)
            {
                ToastNotification.Show($"No se pudo abrir la vista previa: {ex.Message}", ToastType.Error, 4);
            }
        }

        private void LoadQrImage()
        {
            var raw = Transaction.QrCode?.QrImageBase64;
            if (string.IsNullOrWhiteSpace(raw))
                return;

            try
            {
                var base64 = raw;
                var commaIndex = base64.IndexOf(',');
                if (commaIndex >= 0)
                    base64 = base64[(commaIndex + 1)..];

                var bytes = Convert.FromBase64String(base64);
                using var stream = new System.IO.MemoryStream(bytes);

                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();

                QrImage.Source = image;
            }
            catch
            {
            }
        }

        private static string GetPaymentMethodDisplay(string? paymentMethod)
        {
            var text = paymentMethod?.Trim().ToLowerInvariant();
            return text switch
            {
                "cash" => "Efectivo",
                "card" => "Tarjeta",
                "transfer" => "Transferencia",
                null or "" => "No aplica",
                _ => paymentMethod ?? "No aplica"
            };
        }

        private string GetUserDisplay()
        {
            if (!string.IsNullOrWhiteSpace(Transaction.User?.Username))
                return Transaction.User.Username;

            if (Transaction.IdUser.HasValue)
                return $"ID {Transaction.IdUser.Value}";

            return "No aplica";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void CompletePurchaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (Transaction.Status == TransactionStatusEnum.Completed)
            {
                ToastNotification.Show("La transacción ya está completada", ToastType.Info, 3);
                return;
            }

            var confirm = MessageBox.Show(
                $"¿Completar la compra #{Transaction.IdTransaction}?",
                "Confirmar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                CompletePurchaseButton.IsEnabled = false;
                var token = _sessionService.AuthToken ?? string.Empty;
                var result = await _apiService.CompleteTransactionAsync(Transaction.IdTransaction, token);

                if (result.Success)
                {
                    Transaction.Status = TransactionStatusEnum.Completed;
                    ToastNotification.Show("Compra completada correctamente", ToastType.Success, 3);
                    DialogResult = true;
                    Close();
                    return;
                }

                ToastNotification.Show(result.ErrorMessage ?? "No se pudo completar la compra", ToastType.Warning, 4);
                UpdateActionButtons();
            }
            catch (Exception ex)
            {
                ToastNotification.Show($"Error al completar compra: {ex.Message}", ToastType.Error, 4);
                UpdateActionButtons();
            }
        }

        private async void CompleteSpecialServiceButton_Click(object sender, RoutedEventArgs e)
        {
            var pendingProductIds = GetPendingSpecialServiceProductIds();
            if (pendingProductIds.Count == 0)
            {
                ToastNotification.Show("No hay servicios especiales pendientes", ToastType.Info, 3);
                return;
            }

            var confirm = MessageBox.Show(
                $"¿Completar servicio especial para {pendingProductIds.Count} producto(s)?",
                "Confirmar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                CompleteSpecialServiceButton.IsEnabled = false;
                var token = _sessionService.AuthToken ?? string.Empty;
                var allUpdated = true;

                foreach (var productId in pendingProductIds)
                {
                    var specialResult = await _apiService.UpdateProductSpecialServiceStatusAsync(productId, "completed", token);
                    if (!specialResult.Success)
                    {
                        allUpdated = false;
                        ToastNotification.Show(specialResult.ErrorMessage ?? $"No se pudo completar servicio especial del producto {productId}", ToastType.Warning, 4);
                    }
                    else
                    {
                        SetSpecialServiceStatus(productId, "completed");
                    }
                }

                if (allUpdated)
                {
                    ToastNotification.Show($"Servicios especiales completados para {pendingProductIds.Count} producto(s)", ToastType.Success, 4);
                }
                else
                {
                    ToastNotification.Show("Se completó el servicio especial, pero hubo errores en algunos productos", ToastType.Warning, 4);
                }

                UpdateActionButtons();
            }
            catch (Exception ex)
            {
                ToastNotification.Show($"Error al completar servicio especial: {ex.Message}", ToastType.Error, 4);
                UpdateActionButtons();
            }
        }

        private void SetSpecialServiceStatus(int productId, string status)
        {
            if (Transaction.Details == null)
                return;

            foreach (var detail in Transaction.Details.Where(d => d.IdProduct == productId))
            {
                if (detail.Product?.SpecialService != null)
                {
                    detail.Product.SpecialService.Status = status;
                }

                if (detail.Product?.SpecialService?.SpecialService != null)
                {
                    detail.Product.SpecialService.SpecialService.Status = status;
                }
            }
        }

        private int? FindSpecialServiceProductIdByFile(FileData file)
        {
            if (Transaction.Details == null)
                return null;

            var fileHash = file.FileHash;
            if (string.IsNullOrWhiteSpace(fileHash))
                return null;

            foreach (var detail in Transaction.Details)
            {
                var product = detail.Product;
                if (!IsSpecialServiceProduct(product))
                    continue;

                var hashes = new List<string?>();
                if (product.File != null && !string.IsNullOrWhiteSpace(product.File.FileHash))
                {
                    hashes.Add(product.File.FileHash);
                }

                if (product.Files != null)
                {
                    hashes.AddRange(product.Files.Where(f => !string.IsNullOrWhiteSpace(f.FileHash)).Select(f => f.FileHash));
                }

                if (hashes.Any(h => string.Equals(h, fileHash, StringComparison.OrdinalIgnoreCase)))
                    return product.IdProduct;
            }

            return null;
        }
    }

    public class TransactionDetailRow
    {
        public int DetailId { get; set; }
        public int ProductId { get; set; }
        public bool IsSpecialService { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public int Amount { get; set; }
        public decimal Price { get; set; }
        public decimal Subtotal { get; set; }
        public FileData? PreviewFile { get; set; }
        public bool IsPreviewAvailable { get; set; }
        public bool ShowPreviewButton { get; set; }
    }
}
