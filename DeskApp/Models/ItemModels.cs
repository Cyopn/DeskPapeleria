using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeskApp.Models
{
    public class ItemData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("id_item")]
        public int IdItem { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("available")]
        public bool Available { get; set; } = true;

        [JsonPropertyName("category")]
        [JsonConverter(typeof(ItemCategoryConverter))]
        public ItemCategory Category { get; set; } = ItemCategory.Otros;

        [JsonPropertyName("product")]
        public ProductData? Product { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class ItemCreateRequest
    {
        [JsonPropertyName("id_item")]
        public int? IdItem { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("available")]
        public bool Available { get; set; } = true;

        [JsonPropertyName("category")]
        [JsonConverter(typeof(ItemCategoryConverter))]
        public ItemCategory Category { get; set; } = ItemCategory.Otros;
    }

    public class ItemUpdateRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("available")]
        public bool Available { get; set; } = true;

        [JsonPropertyName("category")]
        [JsonConverter(typeof(ItemCategoryConverter))]
        public ItemCategory Category { get; set; } = ItemCategory.Otros;
    }

    public class ItemResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("item")]
        public ItemData? Item { get; set; }
    }

    public enum ItemCategory
    {
        Oficina,
        Papeleria,
        ArteYDiseno,
        Otros
    }

    public class ItemCategoryConverter : JsonConverter<ItemCategory>
    {
        public override ItemCategory Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString()?.Trim() ?? string.Empty;
                return s.ToLower() switch
                {
                    "oficina" => ItemCategory.Oficina,
                    "papeleria" => ItemCategory.Papeleria,
                    "arte_y_diseno" => ItemCategory.ArteYDiseno,
                    "otros" => ItemCategory.Otros,
                    _ => ItemCategory.Otros,
                };
            }
            return ItemCategory.Otros;
        }

        public override void Write(Utf8JsonWriter writer, ItemCategory value, JsonSerializerOptions options)
        {
            var s = value switch
            {
                ItemCategory.Oficina => "oficina",
                ItemCategory.Papeleria => "papeleria",
                ItemCategory.ArteYDiseno => "arte_y_diseno",
                ItemCategory.Otros => "otros",
                _ => "otros",
            };
            writer.WriteStringValue(s);
        }
    }
}
