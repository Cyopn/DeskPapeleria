using System;
using System.Globalization;
using System.Windows.Data;

namespace DeskApp
{
    public class BoolSiNoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? "Sí" : "No";

            if (value is null)
                return "No";

            var text = value.ToString()?.Trim().ToLowerInvariant();
            return text switch
            {
                "true" => "Sí",
                "false" => "No",
                _ => value.ToString() ?? string.Empty
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value?.ToString()?.Trim().ToLowerInvariant();
            return text == "sí" || text == "si";
        }
    }

    public class StatusDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value?.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;
            return text switch
            {
                "pending" => "Pendiente",
                "completed" => "Completada",
                "available" => "Disponible",
                "busy" => "Ocupada",
                "offline" => "Fuera de línea",
                _ => value?.ToString() ?? string.Empty
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value?.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;
            return text switch
            {
                "pendiente" => "pending",
                "completada" => "completed",
                "disponible" => "available",
                "ocupada" => "busy",
                "fuera de línea" => "offline",
                _ => value?.ToString() ?? string.Empty
            };
        }
    }

    public class ConnectionTypeDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value?.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;
            return text switch
            {
                "usb" => "USB",
                "network" => "Red",
                "installed" => "Instalada",
                "unknown" => "No especificado",
                _ => value?.ToString() ?? string.Empty
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value?.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;
            return text switch
            {
                "usb" => "usb",
                "red" => "network",
                "instalada" => "installed",
                "no especificado" => "unknown",
                _ => value?.ToString() ?? string.Empty
            };
        }
    }

    public class PaymentMethodDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value?.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;
            return text switch
            {
                "cash" => "Efectivo",
                "card" => "Tarjeta",
                "transfer" => "Transferencia",
                _ => value?.ToString() ?? string.Empty
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value?.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;
            return text switch
            {
                "efectivo" => "cash",
                "tarjeta" => "card",
                "transferencia" => "transfer",
                _ => value?.ToString() ?? string.Empty
            };
        }
    }
}
