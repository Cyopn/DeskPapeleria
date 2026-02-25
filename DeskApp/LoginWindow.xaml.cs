using System;
using System.Collections.Generic;
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
    /// Lógica de interacción para LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly SessionService _sessionService;
        private bool _isLoggingIn = false;
        private bool _isPasswordVisible = false;

        public LoginWindow()
        {
            InitializeComponent();
            ToastNotification.Initialize(NotificationContainer);
            _apiService = ApiService.Instance;
            _sessionService = SessionService.Instance;

            this.Loaded += LoginWindow_Loaded;
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_sessionService.IsAuthenticated)
            {
                IndexWindow indexWindow = new IndexWindow();
                indexWindow.Show();
                this.Close();
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordHint.Visibility = string.IsNullOrEmpty(PasswordBox.Password) 
                ? Visibility.Visible 
                : Visibility.Collapsed;
            if (!_isPasswordVisible)
            {
                PasswordTextBox.Text = PasswordBox.Password;
            }
        }

        private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PasswordHint.Visibility = string.IsNullOrEmpty(PasswordTextBox.Text) 
                ? Visibility.Visible 
                : Visibility.Collapsed;
            if (_isPasswordVisible)
            {
                PasswordBox.Password = PasswordTextBox.Text;
            }
        }

        private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
                TogglePasswordIcon.Text = "🙈";
                PasswordTextBox.Focus();
                PasswordTextBox.CaretIndex = PasswordTextBox.Text.Length;
            }
            else
            {
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
                TogglePasswordIcon.Text = "👁";
                PasswordBox.Focus();
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoggingIn)
            {
                return;
            }

            if (!ValidateLogin())
            {
                return;
            }

            _isLoggingIn = true;
            var button = sender as Button;
            if (button != null)
            {
                button.IsEnabled = false;
                button.Content = "Iniciando sesión...";
            }

            try
            {
                ToastNotification.Show("Iniciando sesión...", ToastType.Info, 2);

                var password = _isPasswordVisible ? PasswordTextBox.Text : PasswordBox.Password;

                var request = new LoginRequest
                {
                    Username = UsernameTextBox.Text.Trim(),
                    Password = password
                };

                var result = await _apiService.LoginAsync(request);

                if (result.Success && result.Data != null)
                {
                    _sessionService.SetSession(result.Data.Token, result.Data.User!);

                    ToastNotification.Show(
                        $"¡Bienvenido {result.Data.User?.Username}!", 
                        ToastType.Success, 
                        2);
                    await Task.Delay(1500);
                    IndexWindow indexWindow = new IndexWindow();
                    indexWindow.Show();
                    this.Close();
                }
                else
                {
                    HandleLoginError(result);
                }
            }
            catch (Exception ex)
            {
                ToastNotification.Show(
                    $"Error inesperado: {ex.Message}", 
                    ToastType.Error, 
                    5);
            }
            finally
            {
                _isLoggingIn = false;
                if (button != null)
                {
                    button.IsEnabled = true;
                    button.Content = "Iniciar sesión";
                }
            }
        }

        private void HandleLoginError(ApiResult<LoginResponse> result)
        {
            switch (result.StatusCode)
            {
                case 400:
                    if (result.ValidationErrors != null && result.ValidationErrors.Count > 0)
                    {
                        var mainError = result.ErrorMessage ?? "Datos inválidos";
                        var detailedErrors = string.Join("\n• ", result.ValidationErrors);
                        
                        ToastNotification.Show(
                            $"{mainError}\n• {detailedErrors}", 
                            ToastType.Error, 
                            5);
                    }
                    else
                    {
                        ToastNotification.Show(
                            result.ErrorMessage ?? "Datos inválidos", 
                            ToastType.Error, 
                            4);
                    }
                    break;

                case 401:
                    ToastNotification.Show(
                        result.ErrorMessage ?? "Usuario o contraseña incorrectos", 
                        ToastType.Error, 
                        4);
                    if (_isPasswordVisible)
                    {
                        PasswordTextBox.Clear();
                        PasswordTextBox.Focus();
                    }
                    else
                    {
                        PasswordBox.Clear();
                        PasswordBox.Focus();
                    }
                    break;

                case 404:
                    ToastNotification.Show(
                        result.ErrorMessage ?? "Usuario no encontrado", 
                        ToastType.Error, 
                        4);
                    UsernameTextBox.Focus();
                    break;

                case 500:
                    ToastNotification.Show(
                        result.ErrorMessage ?? "Error interno del servidor. Inténtalo más tarde.", 
                        ToastType.Error, 
                        5);
                    break;

                case 0:
                    if (result.ErrorMessage != null && result.ErrorMessage.Contains("tiempo"))
                    {
                        ToastNotification.Show(
                            "La solicitud ha excedido el tiempo de espera. Verifica tu conexión.", 
                            ToastType.Error, 
                            5);
                    }
                    else
                    {
                        ToastNotification.Show(
                            "Error de conexión con el servidor. Verifica tu conexión a Internet.", 
                            ToastType.Error, 
                            5);
                    }
                    break;

                default:
                    ToastNotification.Show(
                        result.ErrorMessage ?? $"Error HTTP {result.StatusCode}. Inténtalo de nuevo.", 
                        ToastType.Error, 
                        4);
                    break;
            }
        }

        private bool ValidateLogin()
        {
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                ToastNotification.Show("El nombre de usuario es obligatorio", ToastType.Error);
                UsernameTextBox.Focus();
                return false;
            }

            var password = _isPasswordVisible ? PasswordTextBox.Text : PasswordBox.Password;
            if (string.IsNullOrWhiteSpace(password))
            {
                ToastNotification.Show("La contraseña es obligatoria", ToastType.Error);
                if (_isPasswordVisible)
                {
                    PasswordTextBox.Focus();
                }
                else
                {
                    PasswordBox.Focus();
                }
                return false;
            }

            return true;
        }

        private void RegisterTextBlock_Click(object sender, MouseButtonEventArgs e)
        {
            RegisterWindow registerWindow = new RegisterWindow();
            registerWindow.Show();
            this.Close();
        }
    }
}
