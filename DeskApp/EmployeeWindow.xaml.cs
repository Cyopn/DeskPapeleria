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
    /// Lógica de interacción para EmployeeWindow.xaml
    /// </summary>
    public partial class EmployeeWindow : Window
    {
        private readonly SessionService _sessionService;
        private readonly ApiService _apiService;
        private Button? _activeTabButton;

        public ObservableCollection<UserData> Users { get; } = new();

        public EmployeeWindow()
        {
            InitializeComponent();
            ToastNotification.Initialize(NotificationContainer);
            _sessionService = SessionService.Instance;
            _apiService = ApiService.Instance;
            DataContext = this;

            // Verificar autenticación
            if (!_sessionService.IsAuthenticated)
            {
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
                return;
            }

            // Establecer el primer tab como activo
            SetActiveTab(EmpleadosButton, Tab1Content);

            // Mostrar mensaje de bienvenida
            if (_sessionService.CurrentUser != null)
            {
                ToastNotification.Show(
                    $"Bienvenido, {_sessionService.CurrentUser.Names}!", 
                    ToastType.Success, 
                    3);
            }

            _ = LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var result = await _apiService.GetUsersAsync(_sessionService.Token ?? string.Empty);
                if (result.Success && result.Data != null)
                {
                    Users.Clear();
                    foreach (var user in result.Data.Where(u => u.Role == "employee" || u.Role == "admin"))
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
            // Ocultar todos los tabs
            Tab1Content.Visibility = Visibility.Collapsed;
            Tab2Content.Visibility = Visibility.Collapsed;
            Tab3Content.Visibility = Visibility.Collapsed;

            // Resetear estilos a los básicos
            var baseStyle = (Style)FindResource("MenuButtonStyle");
            EmpleadosButton.Style = baseStyle;
            ProductosButton.Style = baseStyle;
            ImpresorasButton.Style = baseStyle;

            // Asignar estilo activo
            var activeStyle = (Style)FindResource("ActiveMenuButtonStyle");
            activeButton.Style = activeStyle;

            // Mostrar contenido seleccionado
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
                
                // Esperar un momento para que se vea el toast
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

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is UserData user)
            {
                ToastNotification.Show($"Editar usuario: {user.Names} {user.Lastnames}", ToastType.Info, 2);
            }
        }
    }
}
