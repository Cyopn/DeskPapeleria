using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DeskApp
{
    public class RoleToBrushConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var role = (value as string ?? string.Empty).ToLowerInvariant();
            return role switch
            {
                "admin" => (SolidColorBrush)(new BrushConverter().ConvertFrom("#04DF34")),
                "manager" => (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFE417")),
                "supervisor" => (SolidColorBrush)(new BrushConverter().ConvertFrom("#8392EE")),
                "employee" => (SolidColorBrush)(new BrushConverter().ConvertFrom("#D9D9D9")),
                _ => Brushes.Transparent
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
