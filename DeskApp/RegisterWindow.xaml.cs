using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
using DeskApp.Models;
using DeskApp.Services;

namespace DeskApp
{
    /// <summary>
    /// Lógica de interacción para RegisterWindow.xaml
    /// </summary>
    public partial class RegisterWindow
    {
        private readonly ApiService _apiService;
        private bool _isRegistering = false;
        private bool _isPasswordVisible = false;
        private bool _isPasswordConfirmVisible = false;

        public RegisterWindow()
        {
            InitializeComponent();
            ToastNotification.Initialize(NotificationContainer);
            _apiService = ApiService.Instance;
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

        private void PasswordConfirmBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordConfirmHint.Visibility = string.IsNullOrEmpty(PasswordConfirmBox.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
            if (!_isPasswordConfirmVisible)
            {
                PasswordConfirmTextBox.Text = PasswordConfirmBox.Password;
            }
        }

        private void PasswordConfirmTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PasswordConfirmHint.Visibility = string.IsNullOrEmpty(PasswordConfirmTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
            if (_isPasswordConfirmVisible)
            {
                PasswordConfirmBox.Password = PasswordConfirmTextBox.Text;
            }
        }

        private void TogglePasswordConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordConfirmVisible = !_isPasswordConfirmVisible;

            if (_isPasswordConfirmVisible)
            {
                PasswordConfirmTextBox.Text = PasswordConfirmBox.Password;
                PasswordConfirmBox.Visibility = Visibility.Collapsed;
                PasswordConfirmTextBox.Visibility = Visibility.Visible;
                TogglePasswordConfirmIcon.Text = "🙈";
                PasswordConfirmTextBox.Focus();
                PasswordConfirmTextBox.CaretIndex = PasswordConfirmTextBox.Text.Length;
            }
            else
            {
                PasswordConfirmBox.Password = PasswordConfirmTextBox.Text;
                PasswordConfirmTextBox.Visibility = Visibility.Collapsed;
                PasswordConfirmBox.Visibility = Visibility.Visible;
                TogglePasswordConfirmIcon.Text = "👁";
                PasswordConfirmBox.Focus();
            }
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRegistering)
            {
                return;
            }

            if (!ValidateForm())
            {
                return;
            }

            _isRegistering = true;
            var button = sender as Button;
            if (button != null)
            {
                button.IsEnabled = false;
                button.Content = "Registrando...";
            }

            try
            {
                var password = _isPasswordVisible ? PasswordTextBox.Text : PasswordBox.Password;
                var passwordConfirm = _isPasswordConfirmVisible ? PasswordConfirmTextBox.Text : PasswordConfirmBox.Password;

                var request = new UserRegistrationRequest
                {
                    Username = UsernameTextBox.Text.Trim().ToLower(),
                    Names = NameTextBox.Text.Trim(),
                    Lastnames = LastnameTextBox.Text.Trim(),
                    Email = EmailTextBox.Text.Trim().ToLower(),
                    Password = password,
                    PasswordConfirm = passwordConfirm,
                    Phone = PhoneTextBox.Text.Trim(),
                    Role = "employee"
                };

                var result = await _apiService.RegisterUserAsync(request);

                if (result.Success && result.Data != null)
                {
                    ToastNotification.Show("¡Registro exitoso!", 
                        ToastType.Success, 
                        3);
                    await Task.Delay(2000);
                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.Show();
                    this.Close();
                }
                else
                {
                    HandleRegistrationError(result);
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
                _isRegistering = false;
                if (button != null)
                {
                    button.IsEnabled = true;
                    button.Content = "Registrarse";
                }
            }
        }

        private void HandleRegistrationError(ApiResult<UserRegistrationResponse> result)
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
                            6);
                    }
                    else
                    {
                        ToastNotification.Show(
                            result.ErrorMessage ?? "Datos inválidos. Por favor verifica la información.", 
                            ToastType.Error, 
                            4);
                    }
                    break;

                case 409:
                    ToastNotification.Show(
                        result.ErrorMessage ?? "El usuario o email ya están registrados", 
                        ToastType.Error, 
                        4);
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

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                ToastNotification.Show("El nombre de usuario es obligatorio", ToastType.Error);
                UsernameTextBox.Focus();
                return false;
            }

            if (UsernameTextBox.Text.Trim().Length < 3)
            {
                ToastNotification.Show("El nombre de usuario debe tener al menos 3 caracteres", ToastType.Warning);
                UsernameTextBox.Focus();
                return false;
            }
            if (!IsValidUsername(UsernameTextBox.Text))
            {
                ToastNotification.Show("El nombre de usuario solo puede contener letras, números y guiones bajos", ToastType.Warning);
                UsernameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                ToastNotification.Show("El nombre es obligatorio", ToastType.Error);
                NameTextBox.Focus();
                return false;
            }

            if (NameTextBox.Text.Trim().Length < 2)
            {
                ToastNotification.Show("El nombre debe tener al menos 2 caracteres", ToastType.Warning);
                NameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(LastnameTextBox.Text))
            {
                ToastNotification.Show("Los apellidos son obligatorios", ToastType.Error);
                LastnameTextBox.Focus();
                return false;
            }

            if (LastnameTextBox.Text.Trim().Length < 2)
            {
                ToastNotification.Show("Los apellidos deben tener al menos 2 caracteres", ToastType.Warning);
                LastnameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                ToastNotification.Show("El correo es obligatorio", ToastType.Error);
                EmailTextBox.Focus();
                return false;
            }

            if (!IsValidEmail(EmailTextBox.Text))
            {
                ToastNotification.Show("El correo no es válido", ToastType.Error);
                EmailTextBox.Focus();
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

            if (password.Length < 6)
            {
                ToastNotification.Show("La contraseña debe tener al menos 6 caracteres", ToastType.Warning);
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

            var passwordConfirm = _isPasswordConfirmVisible ? PasswordConfirmTextBox.Text : PasswordConfirmBox.Password;
            if (string.IsNullOrWhiteSpace(passwordConfirm))
            {
                ToastNotification.Show("Debes confirmar la contraseña", ToastType.Error);
                if (_isPasswordConfirmVisible)
                {
                    PasswordConfirmTextBox.Focus();
                }
                else
                {
                    PasswordConfirmBox.Focus();
                }
                return false;
            }

            if (password != passwordConfirm)
            {
                ToastNotification.Show("Las contraseñas no coinciden", ToastType.Error);
                if (_isPasswordConfirmVisible)
                {
                    PasswordConfirmTextBox.Focus();
                }
                else
                {
                    PasswordConfirmBox.Focus();
                }
                return false;
            }

            if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                ToastNotification.Show("El teléfono es obligatorio", ToastType.Error);
                PhoneTextBox.Focus();
                return false;
            }

            if (!IsValidPhone(PhoneTextBox.Text))
            {
                ToastNotification.Show("El teléfono no es válido (mínimo 9 dígitos)", ToastType.Warning);
                PhoneTextBox.Focus();
                return false;
            }

            return true;
        }

        private bool IsValidUsername(string username)
        {
            try
            {
                var regex = new Regex(@"^[a-zA-Z0-9_]+$");
                return regex.IsMatch(username);
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPhone(string phone)
        {
            try
            {
                var cleanPhone = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
                var digitsOnly = new string(cleanPhone.Where(char.IsDigit).ToArray());
                return digitsOnly.Length >= 9;
            }
            catch
            {
                return false;
            }
        }

        private void LoginTextBlock_Click(object sender, MouseButtonEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}
