using System;
using System.Globalization;
using System.Windows.Data;

namespace DeskApp
{
    public class SpecialServiceTypeDisplayConverter : IValueConverter
    {
        public static string Map(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var raw = value.ToString()?.Trim() ?? string.Empty;
            var code = raw;
            var start = raw.IndexOf('(');
            var end = raw.IndexOf(')');
            if (start >= 0 && end > start)
            {
                code = raw.Substring(start + 1, end - start - 1);
            }

            code = code?.Trim().ToLowerInvariant() ?? string.Empty;

            return code switch
            {
                "enc_imp" or "enc_ipm" => "Encuadernado",
                "ani_imp" => "Anillado",
                "doc_esp" => "Documento especial",
                "photo" => "Fotografia",
                _ => raw
            };
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;
            return Map(value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
