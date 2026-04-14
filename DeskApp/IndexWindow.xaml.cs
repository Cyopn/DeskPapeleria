using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ClosedXML.Excel;
using Microsoft.Win32;
using LiveCharts;
using LiveCharts.Wpf;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.Printing;
using System.Management;
using DeskApp.Models;
using DeskApp.Services;

namespace DeskApp
{
    /// <summary>
    /// Lógica de interacción para IndexWindow.xaml
    /// </summary>
    public partial class IndexWindow : Window
    {
        private readonly SessionService _sessionService;
        private readonly ApiService _apiService;
        private Button? _activeTabButton;
        private ICollectionView? _usersView;
        private ICollectionView? _otherUsersView;
        private ICollectionView? _productsView;
        private ICollectionView? _printersView;
        private ICollectionView? _transactionsView;
        private string _transactionSearchField = "all";
        private DashboardMetricsSnapshot? _latestMetrics;

        public ObservableCollection<UserData> Users { get; } = new();
        public ObservableCollection<UserData> OtherUsers { get; } = new();
        public ObservableCollection<ProductData> Products { get; } = new();
        public ObservableCollection<PrinterData> Printers { get; } = new();
        public ObservableCollection<TransactionData> Transactions { get; } = new();

        private SeriesCollection salesByDaySeries;
        private SeriesCollection paymentMethodSeries;
        private SeriesCollection topProductsSeries;
        private SeriesCollection transactionTypeSeries;
        private string[] salesByDayLabels;
        private string[] topProductsLabels;
        private string[] paymentMethodLabels;
        private string[] transactionTypeLabels;

        public IndexWindow()
        {
            InitializeComponent();
            ToastNotification.Initialize(NotificationContainer);
            _sessionService = SessionService.Instance;
            _apiService = ApiService.Instance;
            DataContext = this;
            _usersView = CollectionViewSource.GetDefaultView(Users);
            _usersView.Filter = FilterUsers;
            _otherUsersView = CollectionViewSource.GetDefaultView(OtherUsers);
            _otherUsersView.Filter = FilterOtherUsers;
            _productsView = CollectionViewSource.GetDefaultView(Products);
            _productsView.Filter = FilterProducts;
            _printersView = CollectionViewSource.GetDefaultView(Printers);
            _printersView.Filter = FilterPrinters;
            _transactionsView = CollectionViewSource.GetDefaultView(Transactions);
            _transactionsView.Filter = FilterTransactions;

            if (!_sessionService.IsAuthenticated)
            {
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
                return;
            }
            SetActiveTab(EmpleadosButton, Tab1Content);

            if (_sessionService.CurrentUser != null)
            {
                ToastNotification.Show(
                    $"Bienvenido, {_sessionService.CurrentUser.Username}!", 
                    ToastType.Success, 
                    3);
            }

            _ = LoadUsersAsync();
            _ = LoadProductsAsync();
            _ = LoadPrintersAsync();
            _ = LoadTransactionsAsync();
        }

        private void ReloadDashboardButton_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadDashboardData();
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var result = await _apiService.GetUsersAsync(_sessionService.Token ?? string.Empty);
                if (result.Success && result.Data != null)
                {
                    Users.Clear();
                    OtherUsers.Clear();

                    foreach (var user in result.Data.Where(u => u.Role == "employee" || u.Role == "admin" || u.Role == "manager" || u.Role == "supervisor"))
                    {
                        Users.Add(user);
                    }

                    foreach (var user in result.Data.Where(u => u.Role != "employee" && u.Role != "admin" && u.Role != "manager" && u.Role != "supervisor"))
                    {
                        OtherUsers.Add(user);
                    }

                    _usersView?.Refresh();
                    _otherUsersView?.Refresh();
                }
                else
                {
                    ToastNotification.Show(result.ErrorMessage ?? "No se pudieron cargar los usuarios", ToastType.Error, 4);
                }
            }
            catch (Exception ex)
            {
                ToastNotification.Show($"Error al cargar usuarios: {ex.Message}", ToastType.Error, 4);
            }
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                var token = _sessionService.Token ?? string.Empty;
                var result = await _apiService.GetProductsByTypeAsync("item", token);
                if (result.Success && result.Data != null)
                {
                    Products.Clear();
                    foreach (var p in result.Data)
                    {
                        Products.Add(p);
                    }
                    _productsView?.Refresh();
                }
                else
                {
                    ToastNotification.Show(result.ErrorMessage ?? "No se pudieron cargar los productos", ToastType.Error, 4);
                }
            }
            catch (Exception ex)
            {
                ToastNotification.Show($"Error al cargar productos: {ex.Message}", ToastType.Error, 4);
            }
        }

        public async Task LoadPrintersAsync()
        {
            try
            {
                var token = _sessionService.Token ?? string.Empty;
                var result = await _apiService.GetPrintersAsync(token);
                if (result.Success && result.Data != null)
                {
                    var originalStatuses = result.Data
                        .Where(p => p.IdPrinter > 0)
                        .ToDictionary(p => p.IdPrinter, p => p.Status);

                    var withLocalStatus = ApplyLocalPrinterStatus(result.Data);
                    await SyncPrinterStatusesAsync(withLocalStatus, originalStatuses, token);

                    Printers.Clear();
                    foreach (var p in withLocalStatus)
                    {
                        Printers.Add(p);
                    }
                    _printersView?.Refresh();
                    RefreshPrintersTab();
                }
                else
                {
                    ToastNotification.Show(result.ErrorMessage ?? "No se pudieron cargar las impresoras", ToastType.Warning, 4);
                }
            }
            catch (Exception ex)
            {
                ToastNotification.Show($"Error al cargar impresoras: {ex.Message}", ToastType.Error, 4);
            }
        }

        private async Task SyncPrinterStatusesAsync(IEnumerable<PrinterData> printersWithLocalStatus, Dictionary<int, PrinterStatus> apiStatuses, string token)
        {
            foreach (var printer in printersWithLocalStatus)
            {
                if (printer.IdPrinter <= 0)
                    continue;

                if (!apiStatuses.TryGetValue(printer.IdPrinter, out var apiStatus))
                    continue;

                if (apiStatus == printer.Status)
                    continue;

                try
                {
                    await _apiService.UpdatePrinterAsync(printer.IdPrinter, printer, token);
                }
                catch
                {
                }
            }
        }

        private static List<PrinterData> ApplyLocalPrinterStatus(IEnumerable<PrinterData> apiPrinters)
        {
            var printers = apiPrinters.ToList();

            var wmiStateByName = new Dictionary<string, (bool IsOffline, bool IsBusy)>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name, WorkOffline, PrinterStatus, ExtendedPrinterStatus FROM Win32_Printer");
                foreach (var obj in searcher.Get().OfType<ManagementObject>())
                {
                    var name = obj["Name"]?.ToString();
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    var workOffline = Convert.ToBoolean(obj["WorkOffline"] ?? false);
                    var printerStatus = Convert.ToInt32(obj["PrinterStatus"] ?? 0);
                    var extendedStatus = Convert.ToInt32(obj["ExtendedPrinterStatus"] ?? 0);

                    var isOffline = workOffline || printerStatus == 7 || extendedStatus == 7;
                    var isBusy = printerStatus is 3 or 4 or 5 || extendedStatus is 3 or 4 or 5;

                    wmiStateByName[name] = (isOffline, isBusy);
                }
            }
            catch
            {
            }

            try
            {
                using var printServer = new LocalPrintServer();
                var localQueues = printServer.GetPrintQueues().ToList();

                foreach (var printer in printers)
                {
                    var local = localQueues.FirstOrDefault(q =>
                        (!string.IsNullOrWhiteSpace(printer.Name) && string.Equals(q.Name, printer.Name, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(printer.PortName) && string.Equals(q.QueuePort?.Name, printer.PortName, StringComparison.OrdinalIgnoreCase))
                    );

                    if (local == null)
                    {
                        printer.Status = PrinterStatus.Offline;
                        continue;
                    }

                    local.Refresh();
                    var st = local.QueueStatus;

                    var queueOffline = st.HasFlag(PrintQueueStatus.Offline)
                        || st.HasFlag(PrintQueueStatus.NotAvailable)
                        || st.HasFlag(PrintQueueStatus.PaperOut)
                        || st.HasFlag(PrintQueueStatus.DoorOpen)
                        || st.HasFlag(PrintQueueStatus.Error)
                        || st.HasFlag(PrintQueueStatus.UserIntervention);

                    var queueBusy = st.HasFlag(PrintQueueStatus.Printing)
                        || st.HasFlag(PrintQueueStatus.Busy)
                        || st.HasFlag(PrintQueueStatus.Processing)
                        || st.HasFlag(PrintQueueStatus.IOActive);

                    var wmiOffline = false;
                    var wmiBusy = false;
                    if (!string.IsNullOrWhiteSpace(printer.Name) && wmiStateByName.TryGetValue(printer.Name, out var wmi))
                    {
                        wmiOffline = wmi.IsOffline;
                        wmiBusy = wmi.IsBusy;
                    }

                    var pingOffline = false;
                    var ipToPing = ResolvePrinterIp(printer, local);
                    if (!string.IsNullOrWhiteSpace(ipToPing))
                    {
                        try
                        {
                            using var ping = new Ping();
                            var reply = ping.Send(ipToPing, 700);
                            pingOffline = reply == null || reply.Status != IPStatus.Success;
                        }
                        catch
                        {
                            pingOffline = true;
                        }
                    }

                    if (queueOffline || wmiOffline || pingOffline)
                    {
                        printer.Status = PrinterStatus.Offline;
                    }
                    else if (queueBusy || wmiBusy)
                    {
                        printer.Status = PrinterStatus.Busy;
                    }
                    else
                    {
                        printer.Status = PrinterStatus.Available;
                    }
                }
            }
            catch
            {
            }

            return printers;
        }

        private static string? ResolvePrinterIp(PrinterData printer, PrintQueue local)
        {
            if (!string.IsNullOrWhiteSpace(printer.IP))
                return printer.IP;

            var candidates = new[] { printer.PortName, local.QueuePort?.Name };
            foreach (var c in candidates)
            {
                if (string.IsNullOrWhiteSpace(c))
                    continue;

                var match = Regex.Match(c, @"\b(?:(?:25[0-5]|2[0-4]\d|[01]?\d?\d)\.){3}(?:25[0-5]|2[0-4]\d|[01]?\d?\d)\b");
                if (match.Success)
                    return match.Value;
            }

            return null;
        }

        private async Task LoadTransactionsAsync()
        {
            try
            {
                var token = _sessionService.Token ?? string.Empty;
                var result = await _apiService.GetTransactionsAsync(token);
                if (result.Success && result.Data != null)
                {
                    Transactions.Clear();
                    foreach (var t in result.Data)
                    {
                        Transactions.Add(t);
                    }
                    _transactionsView?.Refresh();
                }
                else
                {
                    ToastNotification.Show(result.ErrorMessage ?? "No se pudieron cargar las transacciones", ToastType.Warning, 4);
                }
            }
            catch (Exception ex)
            {
                ToastNotification.Show($"Error al cargar transacciones: {ex.Message}", ToastType.Error, 4);
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string menuTag)
            {
                switch (menuTag)
                {
                    case "Empleados":
                        SetActiveTab(EmpleadosButton, Tab1Content);
                        break;
                    case "Productos":
                        SetActiveTab(ProductosButton, Tab2Content);
                        break;
                    case "Impresoras":
                        SetActiveTab(ImpresorasButton, Tab3Content);
                        break;
                    case "Transacciones":
                        SetActiveTab(TrasaccionesButton, Tab4Content);
                        break;
                    case "Usuarios":
                        SetActiveTab(UsuariosButton, Tab5Content);
                        break;
                    case "Dashboard":
                        DashboardButton_Click(sender, e);
                        break;
                }
            }
        }

        private void OtrosButton_MouseEnter(object sender, MouseEventArgs e)
        {
        }

        private void OtrosButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        private void SetActiveTab(Button activeButton, UIElement activeContent)
        {
            Tab1Content.Visibility = Visibility.Collapsed;
            Tab2Content.Visibility = Visibility.Collapsed;
            Tab3Content.Visibility = Visibility.Collapsed;
            Tab4Content.Visibility = Visibility.Collapsed;
            Tab5Content.Visibility = Visibility.Collapsed;
            Tab6Content.Visibility = Visibility.Collapsed;
            var baseStyle = (Style)FindResource("MenuButtonStyle");
            EmpleadosButton.Style = baseStyle;
            ProductosButton.Style = baseStyle;
            ImpresorasButton.Style = baseStyle;
            TrasaccionesButton.Style = baseStyle;
            UsuariosButton.Style = baseStyle;
            var activeStyle = (Style)FindResource("ActiveMenuButtonStyle");
            activeButton.Style = activeStyle;
            activeContent.Visibility = Visibility.Visible;
            _activeTabButton = activeButton;
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "¿Estás seguro que deseas cerrar sesión?",
                "Cerrar Sesión",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _sessionService.ClearSession();
                ToastNotification.Show("Sesión cerrada correctamente", ToastType.Success, 2);
                Task.Delay(1000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        LoginWindow loginWindow = new LoginWindow();
                        loginWindow.Show();
                        this.Close();
                    });
                });
            }
        }

        private async void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is UserData user)
            {
                var fromOtherUsersTab = string.Equals(btn.CommandParameter?.ToString(), "other-users", StringComparison.OrdinalIgnoreCase);

                var editWindow = fromOtherUsersTab
                    ? new EditUserWindow(
                        user,
                        new[]
                        {
                            new RoleOption("Por defecto", "default"),
                            new RoleOption("Estudiante", "student"),
                            new RoleOption("Profesor", "professor")
                        },
                        "Editar usuario")
                    : new EditUserWindow(user);

                editWindow.Owner = this;

                var result = editWindow.ShowDialog();
                if (result == true)
                {
                    await LoadUsersAsync();
                }
            }
        }

        private async void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProductData product)
            {
                var editWindow = new EditProductWindow(product)
                {
                    Owner = this
                };

                var result = editWindow.ShowDialog();
                if (result == true)
                {
                    await LoadProductsAsync();
                }
            }
        }

        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new CreateProductWindow()
            {
                Owner = this
            };
            var result = win.ShowDialog();
            _ = LoadProductsAsync();
        }

        private void OpenCreatePrinterWindow_Click(object sender, RoutedEventArgs e)
        {
            var win = new CreatePrinterWindow { Owner = this };
            var result = win.ShowDialog();
        }

        private async void DeletePrinter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrinterData? printer = null;
                if (sender is Button btn && btn.Tag is PrinterData p)
                {
                    printer = p;
                }
                else if (sender is Button b && b.DataContext is PrinterData pd)
                {
                    printer = pd;
                }

                if (printer == null)
                {
                    ToastNotification.Show("No se pudo identificar la impresora", ToastType.Warning, 3);
                    return;
                }

                var confirm = MessageBox.Show(
                    $"¿Deseas eliminar la impresora '{printer.Name}'?",
                    "Confirmar eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                {
                    return;
                }

                if (printer.IdPrinter <= 0)
                {
                    ToastNotification.Show("La impresora no tiene un ID válido para eliminar", ToastType.Error, 4);
                    return;
                }

                var token = _sessionService.Token ?? string.Empty;
                var result = await _apiService.DeletePrinterAsync(printer.IdPrinter, token);

                if (result.Success)
                {
                    ToastNotification.Show("Impresora eliminada correctamente", ToastType.Success, 3);
                    await LoadPrintersAsync();
                }
                else
                {
                    ToastNotification.Show(result.ErrorMessage ?? "No se pudo eliminar la impresora", ToastType.Error, 4);
                }
            }
            catch (Exception ex)
            {
                ToastNotification.Show($"Error al eliminar impresora: {ex.Message}", ToastType.Error, 4);
            }
        }

        private void RefreshPrintersTab()
        {
            var dg = this.FindName("PrintersDataGrid") as DataGrid;
            if (dg == null)
            {
                dg = new DataGrid
                {
                    Name = "PrintersDataGrid",
                    AutoGenerateColumns = false,
                    IsReadOnly = true
                };
                dg.Columns.Add(new DataGridTextColumn { Header = "Nombre", Binding = new System.Windows.Data.Binding("Name") });
                dg.Columns.Add(new DataGridTextColumn { Header = "Tipo", Binding = new System.Windows.Data.Binding("ConnectionType") });
                dg.Columns.Add(new DataGridTextColumn { Header = "IP", Binding = new System.Windows.Data.Binding("IP") });
                dg.Columns.Add(new DataGridTextColumn { Header = "Puerto (raw)", Binding = new System.Windows.Data.Binding("PortName") });
                dg.Columns.Add(new DataGridTextColumn { Header = "Driver", Binding = new System.Windows.Data.Binding("Driver") });
                dg.Columns.Add(new DataGridTextColumn { Header = "Modelo", Binding = new System.Windows.Data.Binding("Model") });

                var container = Tab3Content as Grid;
                if (container != null)
                {
                    Grid.SetRow(dg, 1);
                    container.Children.Add(dg);
                }
            }

            dg.ItemsSource = Printers;
        }

        private async void ReloadUsersButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadUsersAsync();
            ToastNotification.Show("Tabla de empleados recargada", ToastType.Info, 2);
        }

        private async void ReloadOtherUsersButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadUsersAsync();
            ToastNotification.Show("Tabla de usuarios recargada", ToastType.Info, 2);
        }

        private async void ReloadProductsButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadProductsAsync();
            ToastNotification.Show("Tabla de productos recargada", ToastType.Info, 2);
        }

        private async void ReloadPrintersButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadPrintersAsync();
            ToastNotification.Show("Tabla de impresoras recargada", ToastType.Info, 2);
        }

        private async void ReloadTransactionsButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadTransactionsAsync();
            ToastNotification.Show("Tabla de transacciones recargada", ToastType.Info, 2);
        }

        private void EmployeesSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _usersView?.Refresh();
        }

        private void OtherUsersSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _otherUsersView?.Refresh();
        }

        private void EmployeesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void ProductsSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _productsView?.Refresh();
        }

        private void PrintersSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _printersView?.Refresh();
        }

        private void TransactionsSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _transactionsView?.Refresh();
        }

        private void TransactionsFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _transactionsView?.Refresh();
        }

        private void TransactionsDateFilterMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var mode = GetSelectedFilterTag(TransactionsDateFilterModeComboBox);
            if (TransactionsDateToPicker != null)
            {
                TransactionsDateToPicker.Visibility = mode == "range" ? Visibility.Visible : Visibility.Collapsed;
            }

            if (mode == "none")
            {
                if (TransactionsDateFromPicker != null)
                {
                    TransactionsDateFromPicker.SelectedDate = null;
                }

                if (TransactionsDateToPicker != null)
                {
                    TransactionsDateToPicker.SelectedDate = null;
                }
            }

            _transactionsView?.Refresh();
        }

        private void TransactionsDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            _transactionsView?.Refresh();
        }

        private async void TransactionsDataGrid_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGrid)
                return;

            var source = e.OriginalSource as DependencyObject;
            var row = FindVisualParent<DataGridRow>(source);
            if (row?.Item is not TransactionData selected)
                return;

            await OpenTransactionDetailsAsync(selected);
        }

        private async Task OpenTransactionDetailsAsync(TransactionData selected)
        {
            var transactionToShow = selected;

            try
            {
                var token = _sessionService.Token ?? string.Empty;
                var detailResult = await _apiService.GetTransactionByIdAsync(selected.IdTransaction, token);
                if (detailResult.Success && detailResult.Data != null)
                {
                    transactionToShow = detailResult.Data;
                }
                else if (!string.IsNullOrWhiteSpace(detailResult.ErrorMessage))
                {
                    ToastNotification.Show($"No se pudo cargar detalle completo: {detailResult.ErrorMessage}", ToastType.Warning, 3);
                }
            }
            catch (Exception ex)
            {
                ToastNotification.Show($"No se pudo cargar detalle completo: {ex.Message}", ToastType.Warning, 3);
            }

            var detailsWindow = new TransactionDetailsWindow(transactionToShow)
            {
                Owner = this
            };
            var dialogResult = detailsWindow.ShowDialog();
            if (dialogResult == true)
            {
                await LoadTransactionsAsync();
            }
        }

        private async void OpenQrScanWindow_Click(object sender, RoutedEventArgs e)
        {
            var qrWindow = new QrScanWindow
            {
                Owner = this
            };

            if (qrWindow.ShowDialog() != true || qrWindow.ScannedTransaction == null)
                return;

            ToastNotification.Show("QR escaneado exitosamente", ToastType.Success, 2);
            await OpenTransactionDetailsAsync(qrWindow.ScannedTransaction);
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;

                child = VisualTreeHelper.GetParent(child);
            }

            return null;
        }

        private void OpenTransactionSearchConfig_Click(object sender, RoutedEventArgs e)
        {
            var win = new TransactionSearchSettingsWindow(_transactionSearchField)
            {
                Owner = this
            };

            var result = win.ShowDialog();
            if (result == true)
            {
                _transactionSearchField = win.SelectedSearchField;
                _transactionsView?.Refresh();
            }
        }

        private static string GetSelectedFilterTag(ComboBox? comboBox)
        {
            if (comboBox?.SelectedItem is ComboBoxItem item)
            {
                return item.Tag?.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;
            }

            return string.Empty;
        }

        private bool FilterUsers(object obj)
        {
            if (obj is not UserData user)
                return false;

            var search = EmployeesSearchTextBox?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(search))
                return true;

            search = search.ToLowerInvariant();

            var roleDisplay = user.Role?.ToLowerInvariant() switch
            {
                "admin" => "administrador",
                "manager" => "gerente",
                "supervisor" => "supervisor",
                "employee" => "empleado",
                _ => user.Role?.ToLowerInvariant() ?? string.Empty
            };

            return user.IdUser.ToString().Contains(search)
                   || (user.Username?.ToLowerInvariant().Contains(search) ?? false)
                   || (user.Names?.ToLowerInvariant().Contains(search) ?? false)
                   || (user.Lastnames?.ToLowerInvariant().Contains(search) ?? false)
                   || (user.Email?.ToLowerInvariant().Contains(search) ?? false)
                   || (user.Role?.ToLowerInvariant().Contains(search) ?? false)
                   || roleDisplay.Contains(search);
        }

        private bool FilterOtherUsers(object obj)
        {
            if (obj is not UserData user)
                return false;

            var search = OtherUsersSearchTextBox?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(search))
                return true;

            search = search.ToLowerInvariant();

            var roleDisplay = user.Role?.ToLowerInvariant() switch
            {
                "admin" => "administrador",
                "manager" => "gerente",
                "supervisor" => "supervisor",
                "employee" => "empleado",
                _ => user.Role?.ToLowerInvariant() ?? string.Empty
            };

            return user.IdUser.ToString().Contains(search)
                   || (user.Username?.ToLowerInvariant().Contains(search) ?? false)
                   || (user.Names?.ToLowerInvariant().Contains(search) ?? false)
                   || (user.Lastnames?.ToLowerInvariant().Contains(search) ?? false)
                   || (user.Email?.ToLowerInvariant().Contains(search) ?? false)
                   || (user.Role?.ToLowerInvariant().Contains(search) ?? false)
                   || roleDisplay.Contains(search);
        }

        private bool FilterProducts(object obj)
        {
            if (obj is not ProductData product)
                return false;

            var search = ProductsSearchTextBox?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(search))
                return true;

            search = search.ToLowerInvariant();

            var name = product.Item?.Name ?? string.Empty;
            var category = product.Item?.Category.ToString() ?? string.Empty;
            var price = product.Price.ToString("0.##");
            var availability = product.Item?.Available == true ? "disponible" : "no disponible";
            var availabilityBool = product.Item?.Available.ToString() ?? string.Empty;

            return name.ToLowerInvariant().Contains(search)
                   || category.ToLowerInvariant().Contains(search)
                   || price.ToLowerInvariant().Contains(search)
                   || availability.ToLowerInvariant().Contains(search)
                   || availabilityBool.ToLowerInvariant().Contains(search);
        }

        private bool FilterPrinters(object obj)
        {
            if (obj is not PrinterData printer)
                return false;

            var search = PrintersSearchTextBox?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(search))
                return true;

            search = search.ToLowerInvariant();

            var name = printer.Name ?? string.Empty;
            var type = printer.ConnectionType.ToString();
            var port = printer.PortName ?? string.Empty;
            var model = printer.Model ?? string.Empty;
            var status = printer.Status.ToString();

            return name.ToLowerInvariant().Contains(search)
                   || type.ToLowerInvariant().Contains(search)
                   || port.ToLowerInvariant().Contains(search)
                   || model.ToLowerInvariant().Contains(search)
                   || status.ToLowerInvariant().Contains(search);
        }

        private bool FilterTransactions(object obj)
        {
            if (obj is not TransactionData tx)
                return false;

            var selectedType = GetSelectedFilterTag(TransactionsTypeFilterComboBox);
            var selectedStatus = GetSelectedFilterTag(TransactionsStatusFilterComboBox);
            var selectedProductType = GetSelectedFilterTag(TransactionsProductTypeFilterComboBox);

            var type = tx.Type.ToString().ToLowerInvariant();
            var status = tx.Status.ToString().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(selectedType) && type != selectedType)
                return false;

            if (!string.IsNullOrWhiteSpace(selectedStatus) && status != selectedStatus)
                return false;

            if (!string.IsNullOrWhiteSpace(selectedProductType))
            {
                var txProductType = GetTransactionProductTypeTag(tx);
                if (txProductType != selectedProductType)
                    return false;
            }

            if (!MatchesTransactionDateFilter(tx))
                return false;

            var search = TransactionsSearchTextBox?.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(search))
                return true;

            return IsTransactionMatchByConfiguredField(tx, search);
        }

        private static string GetTransactionProductTypeTag(TransactionData tx)
        {
            var details = tx.Details;
            if (details == null || details.Count == 0)
                return string.Empty;

            bool hasItem = details.Any(d => d?.Product != null && (d.Product.Type == ProductTypeEnum.Item || d.Product.Item != null));
            bool hasPrint = details.Any(d => d?.Product != null && (d.Product.Type == ProductTypeEnum.Print || d.Product.Print != null));
            bool hasSpecialService = details.Any(d => d?.Product != null && (d.Product.Type == ProductTypeEnum.SpecialService || d.Product.SpecialService != null));

            if (hasSpecialService)
                return "special_service";

            if (hasPrint && !hasItem && !hasSpecialService)
                return "print";

            if (hasItem && !hasPrint && !hasSpecialService)
                return "item";

            if (hasPrint)
                return "print";

            if (hasItem)
                return "item";

            return string.Empty;
        }

        private bool MatchesTransactionDateFilter(TransactionData tx)
        {
            var mode = GetSelectedFilterTag(TransactionsDateFilterModeComboBox);
            var fromDate = TransactionsDateFromPicker?.SelectedDate?.Date;
            var toDate = TransactionsDateToPicker?.SelectedDate?.Date;
            var txDate = tx.Date.Date;

            return mode switch
            {
                "before" => fromDate == null || txDate <= fromDate.Value,
                "after" => fromDate == null || txDate >= fromDate.Value,
                "range" => (fromDate == null || txDate >= fromDate.Value)
                           && (toDate == null || txDate <= toDate.Value),
                _ => true
            };
        }

        private bool IsTransactionMatchByConfiguredField(TransactionData tx, string search)
        {
            var type = tx.Type.ToString().ToLowerInvariant();
            var status = tx.Status.ToString().ToLowerInvariant();
            var payment = tx.PaymentMethod?.Trim().ToLowerInvariant() ?? string.Empty;
            var paymentDisplay = FormatPaymentMethodDisplay(tx.PaymentMethod).ToLowerInvariant();
            var idTransaction = tx.IdTransaction.ToString();
            var idUserPrimary = tx.IdUser?.ToString() ?? string.Empty;
            var idUserFromUser = tx.User?.IdUser.ToString() ?? string.Empty;
            var idUserFromEntity = tx.User?.Id.ToString() ?? string.Empty;
            var date = tx.Date.ToString("g").ToLowerInvariant();
            var user = (tx.User?.Username ?? string.Empty).ToLowerInvariant();
            var total = tx.Total.ToString("0.##").ToLowerInvariant();

            bool idUserMatches = idUserPrimary.Contains(search)
                                 || idUserFromUser.Contains(search)
                                 || idUserFromEntity.Contains(search);

            bool paymentMatches = payment.Contains(search) || paymentDisplay.Contains(search);

            return _transactionSearchField switch
            {
                "id_transaction" => idTransaction.Contains(search),
                "id_user" => idUserMatches,
                "type" => type.Contains(search),
                "date" => date.Contains(search),
                "user" => user.Contains(search),
                "total" => total.Contains(search),
                "status" => status.Contains(search),
                "payment_method" => paymentMatches,
                _ => idTransaction.ToLowerInvariant().Contains(search)
                     || idUserMatches
                     || type.Contains(search)
                     || date.Contains(search)
                     || user.Contains(search)
                     || total.Contains(search)
                     || status.Contains(search)
                     || paymentMatches
            };
        }

        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTab(UsuariosButton, Tab6Content);
            Tab6Content.Visibility = Visibility.Visible;
            LoadDashboardData();
        }

        private async Task LoadDashboardData()
        {
            try
            {
                var token = _sessionService.Token ?? string.Empty;
                var result = await _apiService.GetTransactionsDetailsAsync(token);
                if (result.Success && result.Data != null)
                {
                    var transactions = result.Data;
                    var (fromDate, toDate, periodLabel) = GetDashboardPeriodFilter();
                    transactions = transactions
                        .Where(t => (!fromDate.HasValue || t.Date.Date >= fromDate.Value)
                                    && (!toDate.HasValue || t.Date.Date <= toDate.Value))
                        .ToList();

                    if (transactions.Count == 0)
                    {
                        DashboardTotalTransactions.Text = $"No hay datos para el período seleccionado ({periodLabel}).";
                        DashboardTotalAmount.Text = string.Empty;
                        DashboardCompletedCount.Text = string.Empty;
                        DashboardPendingCount.Text = string.Empty;
                        return;
                    }

                    DashboardTotalTransactions.Text = $"Total de transacciones: {transactions.Count}";
                    DashboardTotalAmount.Text = $"Monto total: {transactions.Sum(t => t.Total):C}";
                    DashboardCompletedCount.Text = $"Completadas: {transactions.Count(t => t.Status == TransactionStatusEnum.Completed)}";
                    DashboardPendingCount.Text = $"Pendientes: {transactions.Count(t => t.Status == TransactionStatusEnum.Pending)}";

                    var groupedByDay = transactions
                        .GroupBy(t => t.Date.Date)
                        .OrderBy(g => g.Key)
                        .Select(g => new
                        {
                            Date = g.Key,
                            Label = g.Key.ToString("dd/MM"),
                            Total = g.Sum(t => t.Total),
                            Count = g.Count()
                        })
                        .ToList();

                    var hasDayData = groupedByDay.Count > 0;
                    var salesOverTimeList = hasDayData
                        ? groupedByDay.Select(g => (Label: g.Label, Total: g.Total)).ToList()
                        : new List<(string Label, decimal Total)> { ("Sin datos", 0m) };
                    var dayLabels = salesOverTimeList.Select(g => g.Label).ToArray();
                    var dayTotals = salesOverTimeList.Select(g => g.Total).ToList();

                    var avgByDay = hasDayData
                        ? groupedByDay.Select(g => g.Total / Math.Max(g.Count, 1)).ToList()
                        : new List<decimal> { 0m };
                    AverageTicketLineChart.Series = new SeriesCollection
                    {
                        new LineSeries
                        {
                            Title = "Ticket promedio",
                            Values = new ChartValues<decimal>(avgByDay)
                        }
                    };
                    AverageTicketLineChart.AxisX.Clear();
                    AverageTicketLineChart.AxisX.Add(new Axis { Labels = dayLabels, FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(51,51,51)) });
                    AverageTicketLineChart.AxisY.Clear();
                    AverageTicketLineChart.AxisY.Add(new Axis { Title = "Monto (MXN)", LabelFormatter = value => value.ToString("C"), FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(51,51,51)) });

                    var ticketAverage = transactions.Count == 0 ? 0m : transactions.Sum(t => t.Total) / transactions.Count;
                    TicketAverageValue.Text = $"Ingresos promedio (general: {ticketAverage:C})";

                    var accumulatedValues = new ChartValues<decimal>();
                    decimal runningTotal = 0m;
                    foreach (var slice in salesOverTimeList)
                    {
                        runningTotal += slice.Total;
                        accumulatedValues.Add(runningTotal);
                    }
                    var accumulatedList = salesOverTimeList
                        .Select((slice, index) => (slice.Label, Value: accumulatedValues[index]))
                        .ToList();

                    SalesOverTimeLineChart.Series = new SeriesCollection
                    {
                        new LineSeries
                        {
                            Title = "Ventas",
                            Values = new ChartValues<decimal>(dayTotals)
                        }
                    };
                        SalesOverTimeLineChart.AxisX.Clear();
                    SalesOverTimeLineChart.AxisX.Add(new Axis { Title = "Fecha", Labels = dayLabels, FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(51,51,51)) });
                    SalesOverTimeLineChart.AxisY.Clear();
                    SalesOverTimeLineChart.AxisY.Add(new Axis { Title = "Monto (MXN)", LabelFormatter = value => value.ToString("C"), FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(51,51,51)) });

                    AccumulatedSalesLineChart.Series = new SeriesCollection
                    {
                        new LineSeries
                        {
                            Title = "Ingresos acumulados",
                            Values = accumulatedValues,
                            LineSmoothness = 0.3,
                            Fill = new SolidColorBrush(Color.FromArgb(80, 33, 150, 243))
                        }
                    };
                    AccumulatedSalesLineChart.AxisX.Clear();
                    AccumulatedSalesLineChart.AxisX.Add(new Axis { Title = "Fecha", Labels = dayLabels, FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(51,51,51)) });
                    AccumulatedSalesLineChart.AxisY.Clear();
                    AccumulatedSalesLineChart.AxisY.Add(new Axis { Title = "Monto (MXN)", LabelFormatter = value => value.ToString("C"), FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(51,51,51)) });

                    var transactionCountsList = hasDayData
                        ? groupedByDay.Select(g => (g.Label, g.Count)).ToList()
                        : new List<(string Label, int Count)> { ("Sin datos", 0) };
                    var transactionCounts = transactionCountsList.Select(g => g.Count).ToList();
                    TransactionsCountByDayColumnChart.Series = new SeriesCollection
                    {
                        new ColumnSeries
                        {
                            Title = "Transacciones",
                            Values = new ChartValues<int>(transactionCounts)
                        }
                    };
                    TransactionsCountByDayColumnChart.AxisX.Clear();
                    TransactionsCountByDayColumnChart.AxisX.Add(new Axis { Title = "Fecha", Labels = transactionCountsList.Select(x => x.Label).ToArray(), FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(51,51,51)) });
                    TransactionsCountByDayColumnChart.AxisY.Clear();
                    TransactionsCountByDayColumnChart.AxisY.Add(new Axis { Title = "Transacciones", FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(51,51,51)) });

                    var hourLookup = transactions
                        .GroupBy(t => t.Date.Hour)
                        .ToDictionary(g => g.Key, g => g.Sum(t => t.Total));

                    var hours = Enumerable.Range(0, 24)
                        .Select(hour => new
                        {
                            Hour = hour,
                            Total = hourLookup.TryGetValue(hour, out var total) ? total : 0m
                        })
                        .ToList();

                    SalesByHourLineChart.Series = new SeriesCollection
                    {
                        new LineSeries
                        {
                            Title = "Ventas por hora",
                            Values = new ChartValues<decimal>(hours.Select(h => h.Total))
                        }
                    };
                    SalesByHourLineChart.AxisX.Clear();
                    SalesByHourLineChart.AxisX.Add(new Axis { Title = "Hora", Labels = hours.Select(h => h.Hour.ToString("D2") + ":00").ToArray(), FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(51,51,51)) });
                    SalesByHourLineChart.AxisY.Clear();
                    SalesByHourLineChart.AxisY.Add(new Axis { Title = "Monto (MXN)", LabelFormatter = value => value.ToString("C"), FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(51,51,51)) });

                    var details = transactions.SelectMany(t => t.Details ?? Enumerable.Empty<DetailTransactionData>()).ToList();

                    var categories = details
                        .GroupBy(d =>
                        {
                            if (d?.Product?.Item != null)
                                return d.Product.Item.Category.ToString();

                            if (d?.Product != null && (d.Product.Type == ProductTypeEnum.Print || d.Product.Print != null))
                                return "Impresión de documento";

                            if (d?.Product != null && (d.Product.Type == ProductTypeEnum.SpecialService || d.Product.SpecialService != null))
                                return "Servicio especial";

                            return "Sin categoría";
                        })
                         .Select(g => new { Category = g.Key, Quantity = g.Sum(d => d.Amount), Total = g.Sum(d => d.Price * d.Amount) })
                          .OrderByDescending(x => x.Total)
                          .ToList();
                    SalesByCategoryPieChart.Series = new SeriesCollection();
                    foreach (var group in categories)
                    {
                        SalesByCategoryPieChart.Series.Add(new PieSeries { Title = group.Category, Values = new ChartValues<decimal> { group.Total } });
                    }

                    var paymentGroups = transactions
                        .GroupBy(t => FormatPaymentMethodDisplay(t.PaymentMethod))
                         .Select(g => new { Method = g.Key, Total = g.Sum(t => t.Total) })
                         .OrderByDescending(x => x.Total)
                         .ToList();
                    SalesByPaymentMethodPieChart.Series = new SeriesCollection();
                    foreach (var group in paymentGroups)
                    {
                        SalesByPaymentMethodPieChart.Series.Add(new PieSeries { Title = group.Method, Values = new ChartValues<decimal> { group.Total }, DataLabels = true, LabelPoint = chartPoint => string.Format("{0:N2}%", chartPoint.Participation * 100) });
                    }

                    var topProductsByQuantity = details
                        .GroupBy(d => d?.Product?.Item?.Name ?? d?.Product?.Description ?? "Sin nombre")
                        .Select(g => (Product: g.Key, Quantity: g.Sum(d => d.Amount)))
                        .OrderByDescending(x => x.Quantity)
                        .Take(10)
                        .ToList();
                    if (!topProductsByQuantity.Any())
                    {
                        topProductsByQuantity = new List<(string Product, int Quantity)> { ("Sin datos", 0) };
                    }
                    TopProductsBarHorizontalChart.Series = new SeriesCollection
                    {
                        new RowSeries
                        {
                            Title = "Cantidad",
                            Values = new ChartValues<int>(topProductsByQuantity.Select(x => x.Quantity))
                        }
                    };
                    TopProductsBarHorizontalChart.AxisY.Clear();
                    TopProductsBarHorizontalChart.AxisY.Add(new Axis { Title = "Producto", Labels = topProductsByQuantity.Select(x => x.Product).ToArray(), FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(51,51,51)) });
                    TopProductsBarHorizontalChart.AxisX.Clear();
                    TopProductsBarHorizontalChart.AxisX.Add(new Axis { Title = "Cantidad", FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(51,51,51)) });

                    var incomeByProduct = details
                        .GroupBy(d =>
                        {
                            if (!string.IsNullOrWhiteSpace(d?.Product?.Item?.Name))
                                return d.Product.Item.Name!;

                            if (d?.Product != null && (d.Product.Type == ProductTypeEnum.Print || d.Product.Print != null))
                                return "Impresión de documento";

                            if (d?.Product != null && (d.Product.Type == ProductTypeEnum.SpecialService || d.Product.SpecialService != null))
                                return "Servicio especial";

                            return d?.Product?.Description ?? "Sin nombre";
                        })
                         .Select(g => (Product: g.Key, Quantity: g.Sum(d => d.Amount), Total: g.Sum(d => d.Price * d.Amount)))
                          .OrderByDescending(x => x.Total)
                          .Take(10)
                          .ToList();
                    if (!incomeByProduct.Any())
                    {
                        incomeByProduct = new List<(string Product, int Quantity, decimal Total)> { ("Sin datos", 0, 0m) };
                    }
                    IncomeByProductBarChart.Series = new SeriesCollection
                    {
                        new ColumnSeries
                        {
                            Title = "Ingresos",
                            Values = new ChartValues<decimal>(incomeByProduct.Select(x => x.Total))
                        }
                    };
                    IncomeByProductBarChart.AxisX.Clear();
                    IncomeByProductBarChart.AxisX.Add(new Axis { Title = "Producto", Labels = incomeByProduct.Select(x => x.Product).ToArray(), FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(51,51,51)) });
                    IncomeByProductBarChart.AxisY.Clear();
                    IncomeByProductBarChart.AxisY.Add(new Axis { Title = "Monto (MXN)", LabelFormatter = value => value.ToString("C"), FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(51,51,51)) });

                    var histogramRanges = new[]
                    {
                        (Label: "$0–50", Min: 0m, Max: 50m),
                        (Label: "$50–100", Min: 50m, Max: 100m),
                        (Label: "$100–300", Min: 100m, Max: 300m),
                        (Label: "> $300", Min: 300m, Max: decimal.MaxValue)
                    };
                    var histogramData = histogramRanges
                        .Select(range => new
                        {
                            range.Label,
                            Count = transactions.Count(t => t.Total >= range.Min && (range.Max == decimal.MaxValue ? t.Total >= range.Min : t.Total < range.Max))
                        })
                        .ToList();
                    var transactionRows = transactions
                        .Select(t => new DashboardMetricsSnapshot.TransactionRow
                        {
                            Id = t.IdTransaction,
                            Type = t.Type.ToString(),
                            Product = t.ProductTypeDisplay,
                            Date = t.Date.ToString("g"),
                            User = FormatTransactionUserDisplay(t),
                            Total = t.Total,
                            Status = FormatTransactionStatusDisplay(t.Status),
                            PaymentMethod = FormatPaymentMethodDisplay(t.PaymentMethod)
                         })
                         .ToList();
                     PurchaseSizeHistogramChart.Series = new SeriesCollection
                     {
                         new ColumnSeries
                         {
                             Title = "Cantidad",
                             Values = new ChartValues<int>(histogramData.Select(x => x.Count))
                         }
                     };
                    PurchaseSizeHistogramChart.AxisX.Clear();
                    PurchaseSizeHistogramChart.AxisX.Add(new Axis { Title = "Rango", Labels = histogramData.Select(x => x.Label).ToArray(), FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(51,51,51)) });
                    PurchaseSizeHistogramChart.AxisY.Clear();
                    PurchaseSizeHistogramChart.AxisY.Add(new Axis { Title = "Cantidad", FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(51,51,51)) });

                    _latestMetrics = new DashboardMetricsSnapshot
                    {
                        GeneratedAt = DateTime.Now,
                        TotalTransactions = transactions.Count,
                        TotalAmount = transactions.Sum(t => t.Total),
                        TicketAverage = ticketAverage,
                        SalesOverTime = salesOverTimeList,
                        AccumulatedSales = accumulatedList,
                        SalesByHour = hours.Select(h => ($"{h.Hour:D2}:00", h.Total)).ToList(),
                        SalesByCategory = categories.Select(c => (c.Category, c.Total)).ToList(),
                        PaymentMethods = paymentGroups.Select(p => (p.Method, p.Total)).ToList(),
                        TopProducts = topProductsByQuantity.ToList(),
                        IncomeByProduct = incomeByProduct.Select(x => (x.Product, x.Total)).ToList(),
                        SalesByCategoryDetailed = categories.Select(c => (c.Category, c.Quantity, c.Total)).ToList(),
                        IncomeByProductDetailed = incomeByProduct.Select(x => (x.Product, x.Quantity, x.Total)).ToList(),
                        TransactionsByDay = transactionCountsList.ToList(),
                        PurchaseSizeDistribution = histogramData.Select(x => (x.Label, x.Count)).ToList(),
                        TransactionsLog = transactionRows
                     };
                }
                else
                {
                    DashboardTotalTransactions.Text = "No se pudo cargar el dashboard.";
                }
            }
            catch (Exception ex)
            {
                DashboardTotalTransactions.Text = $"Error: {ex.Message}";
            }
        }

        private void ExportMetricsToExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_latestMetrics == null)
            {
                ToastNotification.Show("Carga el dashboard antes de exportar métricas.", ToastType.Warning, 3);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Excel workbook (*.xlsx)|*.xlsx",
                FileName = $"metricas-dashboard-{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                using var workbook = new XLWorkbook();
                var summarySheet = workbook.AddWorksheet("Resumen");
                summarySheet.Cell(1, 1).Value = "Métrica";
                summarySheet.Cell(1, 2).Value = "Valor";
                summarySheet.Cell(2, 1).Value = "Total transacciones";
                summarySheet.Cell(2, 2).Value = _latestMetrics.TotalTransactions;
                summarySheet.Cell(3, 1).Value = "Ingresos totales";
                summarySheet.Cell(3, 2).Value = _latestMetrics.TotalAmount;
                summarySheet.Cell(4, 1).Value = "Ticket promedio";
                summarySheet.Cell(4, 2).Value = _latestMetrics.TicketAverage;
                summarySheet.Cell(5, 1).Value = "Generado el";
                summarySheet.Cell(5, 2).Value = _latestMetrics.GeneratedAt.ToString("g");

                summarySheet.Cell(7, 1).Value = "Tamaño de compra";
                summarySheet.Cell(8, 1).Value = "Rango";
                summarySheet.Cell(8, 2).Value = "Cantidad";

                var summaryRow = 9;
                foreach (var item in _latestMetrics.PurchaseSizeDistribution)
                {
                    summarySheet.Cell(summaryRow, 1).Value = item.Label;
                    summarySheet.Cell(summaryRow, 2).Value = item.Count;
                    summaryRow++;
                }
                 summarySheet.Columns().AdjustToContents();

                void FillTransactionSheet(IEnumerable<DashboardMetricsSnapshot.TransactionRow> values)
                {
                    var sheet = workbook.AddWorksheet("Transacciones");
                    var headers = new[] { "ID", "Tipo", "Producto", "Fecha", "Usuario", "Total", "Estado", "Método pago" };
                    for (var i = 0; i < headers.Length; i++)
                    {
                        sheet.Cell(1, i + 1).Value = headers[i];
                    }

                    var row = 2;
                    foreach (var item in values)
                    {
                        sheet.Cell(row, 1).Value = item.Id;
                        sheet.Cell(row, 2).Value = item.Type;
                        sheet.Cell(row, 3).Value = item.Product;
                        sheet.Cell(row, 4).Value = item.Date;
                        sheet.Cell(row, 5).Value = item.User;
                        sheet.Cell(row, 6).Value = item.Total;
                        sheet.Cell(row, 7).Value = item.Status;
                        sheet.Cell(row, 8).Value = item.PaymentMethod;
                        row++;
                    }

                    sheet.Column(6).Style.NumberFormat.Format = "$ #,##0.00";
                    sheet.Columns().AdjustToContents();
                }

                void FillTemporalSheet(
                    string title,
                    IEnumerable<(string Label, decimal Value)> sales,
                    IEnumerable<(string Label, int Value)> transactions,
                    IEnumerable<(string Label, decimal Value)> accumulated)
                {
                    var sheet = workbook.AddWorksheet(title);
                    sheet.Cell(1, 1).Value = "Fecha";
                    sheet.Cell(1, 2).Value = "Ventas";
                    sheet.Cell(1, 3).Value = "Transacciones";
                    sheet.Cell(1, 4).Value = "Ingresos acumulados";

                    var salesDict = sales.ToDictionary(x => x.Label, x => x.Value);
                    var transactionsDict = transactions.ToDictionary(x => x.Label, x => x.Value);
                    var accumulatedDict = accumulated.ToDictionary(x => x.Label, x => x.Value);

                    var labels = new List<string>();
                    var seen = new HashSet<string>();

                    void AddLabels(IEnumerable<string> source)
                    {
                        foreach (var label in source)
                        {
                            if (label == null || !seen.Add(label))
                                continue;

                            labels.Add(label);
                        }
                    }

                    AddLabels(salesDict.Keys);
                    AddLabels(transactionsDict.Keys);
                    AddLabels(accumulatedDict.Keys);

                    var row = 2;
                    foreach (var label in labels)
                    {
                        sheet.Cell(row, 1).Value = label;
                        sheet.Cell(row, 2).Value = salesDict.TryGetValue(label, out var salesValue) ? salesValue : 0m;
                        sheet.Cell(row, 3).Value = transactionsDict.TryGetValue(label, out var txValue) ? txValue : 0;
                        sheet.Cell(row, 4).Value = accumulatedDict.TryGetValue(label, out var accValue) ? accValue : 0m;
                        row++;
                    }

                    sheet.Columns(2, 2).Style.NumberFormat.Format = "$ #,##0.00";
                    sheet.Columns(4, 4).Style.NumberFormat.Format = "$ #,##0.00";
                    sheet.Columns(1, 4).AdjustToContents();
                }

                void FillQuantityDecimalSheet(string title, string columnA, string columnB, string columnC, IEnumerable<(string Label, int Quantity, decimal Total)> values)
                {
                    var sheet = workbook.AddWorksheet(title);
                    sheet.Cell(1, 1).Value = columnA;
                    sheet.Cell(1, 2).Value = columnB;
                    sheet.Cell(1, 3).Value = columnC;
                    var row = 2;
                    foreach (var value in values)
                    {
                        sheet.Cell(row, 1).Value = value.Label;
                        sheet.Cell(row, 2).Value = value.Quantity;
                        sheet.Cell(row, 3).Value = value.Total;
                        row++;
                    }
                    sheet.Column(3).Style.NumberFormat.Format = "$ #,##0.00";
                    sheet.Columns(1, 3).AdjustToContents();
                }

                FillTemporalSheet("Evolución temporal", _latestMetrics.SalesOverTime, _latestMetrics.TransactionsByDay, _latestMetrics.AccumulatedSales);
                FillQuantityDecimalSheet("Ventas por categoría", "Categoría", "Cantidad", "Ingresos", _latestMetrics.SalesByCategoryDetailed);
                FillQuantityDecimalSheet("Ventas por producto", "Producto", "Cantidad", "Ingresos", _latestMetrics.IncomeByProductDetailed.Select(p => (p.Product, p.Quantity, p.Revenue)));
                FillTransactionSheet(_latestMetrics.TransactionsLog);
 
                workbook.SaveAs(dialog.FileName);
                ToastNotification.Show("Métricas exportadas a Excel correctamente.", ToastType.Success, 3);
            }
            catch (Exception ex)
            {
                ToastNotification.Show($"No se pudo exportar el archivo: {ex.Message}", ToastType.Error, 4);
            }
        }

        private static string FormatTransactionStatusDisplay(TransactionStatusEnum status)
        {
            return status switch
            {
                TransactionStatusEnum.Pending => "Pendiente",
                TransactionStatusEnum.Completed => "Completada",
                _ => status.ToString()
            };
        }

        private static string FormatTransactionUserDisplay(TransactionData transaction)
        {
            var userId = transaction.User?.IdUser ?? transaction.IdUser;
            var username = transaction.User?.Username;
            var userIdText = userId?.ToString() ?? "Sin id";
            return $"{userIdText}";
        }

        private static string FormatPaymentMethodDisplay(string? paymentMethod)
        {
            var text = paymentMethod?.Trim().ToLowerInvariant();
            return text switch
            {
                "cash" => "Efectivo",
                "credit_card" => "Tarjeta de crédito",
                "paypal" => "PayPal",
                "card" => "Tarjeta de crédito",
                "transfer" => "Transferencia",
                null or "" => "Sin método",
                _ => paymentMethod ?? "Sin método"
            };
        }

        private (DateTime? From, DateTime? To, string Label) GetDashboardPeriodFilter()
        {
            var mode = GetSelectedFilterTag(DashboardPeriodFilterComboBox);
            var today = DateTime.Today;

            return mode switch
            {
                "today" => (today, today, "Hoy"),
                "week" => (today.AddDays(-((7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7)), today, "Esta semana"),
                "month" => (new DateTime(today.Year, today.Month, 1), today, "Este mes"),
                "range" => (
                    DashboardFromDatePicker?.SelectedDate?.Date,
                    DashboardToDatePicker?.SelectedDate?.Date,
                    BuildCustomRangeLabel(DashboardFromDatePicker?.SelectedDate?.Date, DashboardToDatePicker?.SelectedDate?.Date)
                ),
                _ => (null, null, "Todo el historial")
            };
        }

        private static string BuildCustomRangeLabel(DateTime? fromDate, DateTime? toDate)
        {
            if (fromDate.HasValue && toDate.HasValue)
                return $"{fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}";
            if (fromDate.HasValue)
                return $"Desde {fromDate:dd/MM/yyyy}";
            if (toDate.HasValue)
                return $"Hasta {toDate:dd/MM/yyyy}";
            return "Rango personalizado";
        }

        private void DashboardPeriodFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_sessionService == null || _apiService == null)
                return;

            var mode = GetSelectedFilterTag(DashboardPeriodFilterComboBox);
            var isRange = mode == "range";

            if (DashboardFromDatePicker != null)
                DashboardFromDatePicker.Visibility = isRange ? Visibility.Visible : Visibility.Collapsed;

            if (DashboardToDatePicker != null)
                DashboardToDatePicker.Visibility = isRange ? Visibility.Visible : Visibility.Collapsed;

            if (!isRange)
            {
                if (DashboardFromDatePicker != null) DashboardFromDatePicker.SelectedDate = null;
                if (DashboardToDatePicker != null) DashboardToDatePicker.SelectedDate = null;
            }

            if (Tab6Content.Visibility == Visibility.Visible)
                LoadDashboardData();
        }

        private void DashboardPeriodDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Tab6Content.Visibility == Visibility.Visible)
                LoadDashboardData();
        }
    }
 }
