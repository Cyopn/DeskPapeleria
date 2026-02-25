using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public ObservableCollection<UserData> Users { get; } = new();
        public ObservableCollection<ProductData> Products { get; } = new();

        public IndexWindow()
        {
            InitializeComponent();
            ToastNotification.Initialize(NotificationContainer);
            _sessionService = SessionService.Instance;
            _apiService = ApiService.Instance;
            DataContext = this;

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
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var result = await _apiService.GetUsersAsync(_sessionService.Token ?? string.Empty);
                if (result.Success && result.Data != null)
                {
                    Users.Clear();
                    foreach (var user in result.Data.Where(u => u.Role == "employee" || u.Role == "admin" || u.Role == "manager" || u.Role == "supervisor"))
                    {
                        Users.Add(user);
                    }
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
                }
            }
        }

        private void SetActiveTab(Button activeButton, UIElement activeContent)
        {
            Tab1Content.Visibility = Visibility.Collapsed;
            Tab2Content.Visibility = Visibility.Collapsed;
            Tab3Content.Visibility = Visibility.Collapsed;
            var baseStyle = (Style)FindResource("MenuButtonStyle");
            EmpleadosButton.Style = baseStyle;
            ProductosButton.Style = baseStyle;
            ImpresorasButton.Style = baseStyle;
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
                var editWindow = new EditUserWindow(user)
                {
                    Owner = this
                };

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
            win.ShowDialog();
            _ = LoadProductsAsync();
        }
    }
}
