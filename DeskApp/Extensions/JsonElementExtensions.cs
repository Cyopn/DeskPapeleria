using System.Text.Json;

namespace DeskApp.Extensions
{
    public static class JsonElementExtensions
    {
        public static JsonElement GetFirstOrSelf(this JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array && element.GetArrayLength() > 0)
            {
                return element[0];
            }
            return element;
        }

        public static JsonElement? GetPropertyOrDefault(this JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object) return null;
            if (element.TryGetProperty(propertyName, out var prop)) return prop;
            return null;
        }

        public static JsonElement? GetPropertyOrDefault(this JsonElement element, string[] propertyNames)
        {
            if (element.ValueKind != JsonValueKind.Object) return null;
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop)) return prop;
            }
            return null;
        }
    }
}
