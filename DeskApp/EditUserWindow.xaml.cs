using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DeskApp.Models;
using DeskApp.Services;

namespace DeskApp
{
    public partial class EditUserWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly SessionService _sessionService;
        private readonly UserData _user;
        private readonly RoleOption[] _roleOptions = new[]
        {
            new RoleOption("Administrador", "admin"),
            new RoleOption("Gerente", "manager"),
            new RoleOption("Supervisor", "supervisor"),
            new RoleOption("Empleado", "employee")
        };

        public EditUserWindow(UserData user)
        {
            InitializeComponent();
            _apiService = ApiService.Instance;
            _sessionService = SessionService.Instance;
            _user = user;

            RoleComboBox.ItemsSource = _roleOptions;
            RoleComboBox.SelectedValuePath = "Value";
            RoleComboBox.DisplayMemberPath = "Display";

            LoadUserData();
        }

        private void LoadUserData()
        {
            UsernameTextBox.Text = _user.Username;
            NamesTextBox.Text = _user.Names;
            LastnamesTextBox.Text = _user.Lastnames;
            EmailTextBox.Text = _user.Email;
            PhoneTextBox.Text = _user.Phone;

            var match = _roleOptions.FirstOrDefault(r => r.Value == _user.Role);
            if (match != null)
            {
                RoleComboBox.SelectedItem = match;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show("¿Deseas guardar los cambios en este usuario?", "Confirmar guardar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            var selectedRole = RoleComboBox.SelectedValue?.ToString() ?? _user.Role;

            var request = new UserUpdateRequest
            {
                Username = UsernameTextBox.Text.Trim(),
                Names = NamesTextBox.Text.Trim(),
                Lastnames = LastnamesTextBox.Text.Trim(),
                Email = EmailTextBox.Text.Trim(),
                Role = selectedRole,
                Phone = PhoneTextBox.Text.Trim()
            };

            var token = _sessionService.Token ?? string.Empty;
            var result = await _apiService.UpdateUserAsync(_user.IdUser, request, token);

            if (result.Success)
            {
                ToastNotification.Show("Usuario actualizado", ToastType.Success, 2);
                DialogResult = true;
                Close();
            }
            else
            {
                ToastNotification.Show(result.ErrorMessage ?? "No se pudo actualizar el usuario", ToastType.Error, 4);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("¿Estás seguro de que deseas eliminar este usuario?", "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            var token = _sessionService.Token ?? string.Empty;
            var apiResult = await _apiService.DeleteUserAsync(_user.IdUser, token);

            if (apiResult.Success)
            {
                ToastNotification.Show("Usuario eliminado", ToastType.Success, 2);
                DialogResult = true;
                Close();
            }
            else
            {
                ToastNotification.Show(apiResult.ErrorMessage ?? "No se pudo eliminar el usuario", ToastType.Error, 4);
            }
        }
    }

    public record RoleOption(string Display, string Value);
}
