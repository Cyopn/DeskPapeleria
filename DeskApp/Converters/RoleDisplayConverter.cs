using System;
using System.Globalization;
using System.Windows.Data;

namespace DeskApp
{
    public class RoleDisplayConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var role = value as string;
            return role?.ToLowerInvariant() switch
            {
                "admin" => "Administrador",
                "manager" => "Gerente",
                "supervisor" => "Supervisor",
                "employee" => "Empleado",
                _ => role ?? string.Empty
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var display = value as string;
            return display?.ToLowerInvariant() switch
            {
                "administrador" => "admin",
                "gerente" => "manager",
                "supervisor" => "supervisor",
                "empleado" => "employee",
                _ => display ?? string.Empty
            };
        }
    }
}
