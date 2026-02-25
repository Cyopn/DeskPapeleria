using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeskApp.Models
{
    public enum SpecialServiceTypeEnum
    {
        Unknown,
        Cleaning,
        Repair,
        Delivery
    }

    public enum SpecialServiceModeEnum
    {
        Unknown,
        Standard,
        Express
    }

    public class SpecialServiceTypes
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("type")]
        [JsonConverter(typeof(SpecialServiceTypeConverter))]
        public SpecialServiceTypeEnum Type { get; set; }

        [JsonPropertyName("mode")]
        [JsonConverter(typeof(SpecialServiceModeConverter))]
        public SpecialServiceModeEnum Mode { get; set; }

        [JsonPropertyName("delivery")]
        public DateTime Delivery { get; set; }

        [JsonPropertyName("observations")]
        public string? Observations { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class SpecialServiceTypeConverter : JsonConverter<SpecialServiceTypeEnum>
    {
        public override SpecialServiceTypeEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    var value = reader.GetString();
                    if (string.IsNullOrWhiteSpace(value))
                        return SpecialServiceTypeEnum.Unknown;

                    if (Enum.TryParse<SpecialServiceTypeEnum>(value.Trim(), true, out var result))
                        return result;

                    return SpecialServiceTypeEnum.Unknown;
                }

                if (reader.TokenType == JsonTokenType.Number)
                {
                    if (reader.TryGetInt32(out int intValue))
                    {
                        if (Enum.IsDefined(typeof(SpecialServiceTypeEnum), intValue))
                            return (SpecialServiceTypeEnum)intValue;
                    }
                    return SpecialServiceTypeEnum.Unknown;
                }

                if (reader.TokenType == JsonTokenType.Null)
                {
                    return SpecialServiceTypeEnum.Unknown;
                }
            }
            catch (Exception ex)
            {
                throw new JsonException("Error parsing SpecialServiceTypeEnum", ex);
            }

            throw new JsonException("Valor no válido para SpecialServiceTypeEnum");
        }

        public override void Write(Utf8JsonWriter writer, SpecialServiceTypeEnum value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    public class SpecialServiceModeConverter : JsonConverter<SpecialServiceModeEnum>
    {
        public override SpecialServiceModeEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    var value = reader.GetString();
                    if (string.IsNullOrWhiteSpace(value))
                        return SpecialServiceModeEnum.Unknown;

                    if (Enum.TryParse<SpecialServiceModeEnum>(value.Trim(), true, out var result))
                        return result;

                    return SpecialServiceModeEnum.Unknown;
                }

                if (reader.TokenType == JsonTokenType.Number)
                {
                    if (reader.TryGetInt32(out int intValue))
                    {
                        if (Enum.IsDefined(typeof(SpecialServiceModeEnum), intValue))
                            return (SpecialServiceModeEnum)intValue;
                    }
                    return SpecialServiceModeEnum.Unknown;
                }

                if (reader.TokenType == JsonTokenType.Null)
                {
                    return SpecialServiceModeEnum.Unknown;
                }
            }
            catch (Exception ex)
            {
                throw new JsonException("Error parsing SpecialServiceModeEnum", ex);
            }

            throw new JsonException("Valor no válido para SpecialServiceModeEnum");
        }

        public override void Write(Utf8JsonWriter writer, SpecialServiceModeEnum value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
