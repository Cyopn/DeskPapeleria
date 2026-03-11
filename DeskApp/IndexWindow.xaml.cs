using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Printing;
using System.Management;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
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

        public ObservableCollection<UserData> Users { get; } = new();
        public ObservableCollection<UserData> OtherUsers { get; } = new();
        public ObservableCollection<ProductData> Products { get; } = new();
        public ObservableCollection<PrinterData> Printers { get; } = new();
        public ObservableCollection<TransactionData> Transactions { get; } = new();

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
                }
            }
        }

        private void OtrosButton_MouseEnter(object sender, MouseEventArgs e)
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

            return _transactionSearchField switch
            {
                "id_transaction" => idTransaction.Contains(search),
                "id_user" => idUserMatches,
                "type" => type.Contains(search),
                "date" => date.Contains(search),
                "user" => user.Contains(search),
                "total" => total.Contains(search),
                "status" => status.Contains(search),
                "payment_method" => payment.Contains(search),
                _ => idTransaction.ToLowerInvariant().Contains(search)
                     || idUserMatches
                     || type.Contains(search)
                     || date.Contains(search)
                     || user.Contains(search)
                     || total.Contains(search)
                     || status.Contains(search)
                     || payment.Contains(search)
            };
        }

    }
}
