using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeskApp
{
    public enum ProductTypeEnum
    {
        Item,
        Print,
        SpecialService
    }

    public enum PrintStatusEnum
    {
        Pending,
        InProgress,
        Completed
    }

    public class ProductTypeConverter : JsonConverter<ProductTypeEnum>
    {
        public override ProductTypeEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString()?.ToLowerInvariant() ?? string.Empty;
            return s switch
            {
                "item" => ProductTypeEnum.Item,
                "print" => ProductTypeEnum.Print,
                "special_service" => ProductTypeEnum.SpecialService,
                _ => throw new JsonException($"Unknown product type: {s}")
            };
        }

        public override void Write(Utf8JsonWriter writer, ProductTypeEnum value, JsonSerializerOptions options)
        {
            var s = value switch
            {
                ProductTypeEnum.Item => "item",
                ProductTypeEnum.Print => "print",
                ProductTypeEnum.SpecialService => "special_service",
                _ => string.Empty
            };
            writer.WriteStringValue(s);
        }
    }

    public class PrintStatusConverter : JsonConverter<PrintStatusEnum>
    {
        public override PrintStatusEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString()?.ToLowerInvariant() ?? string.Empty;
            return s switch
            {
                "pending" => PrintStatusEnum.Pending,
                "in_progress" => PrintStatusEnum.InProgress,
                "completed" => PrintStatusEnum.Completed,
                _ => throw new JsonException($"Unknown print status: {s}")
            };
        }

        public override void Write(Utf8JsonWriter writer, PrintStatusEnum value, JsonSerializerOptions options)
        {
            var s = value switch
            {
                PrintStatusEnum.Pending => "pending",
                PrintStatusEnum.InProgress => "in_progress",
                PrintStatusEnum.Completed => "completed",
                _ => string.Empty
            };
            writer.WriteStringValue(s);
        }
    }

    public enum CoverTypeEnum
    {
        Hard,
        Soft
    }

    public enum CoverColorEnum
    {
        Red,
        Green,
        Blue,
        Yellow
    }

    public enum SpiralEnum
    {
        Plastic,
        Gluing
    }

    public class CoverTypeConverter : JsonConverter<CoverTypeEnum>
    {
        public override CoverTypeEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString()?.ToLowerInvariant() ?? string.Empty;
            return s switch
            {
                "hard" => CoverTypeEnum.Hard,
                "soft" => CoverTypeEnum.Soft,
                _ => throw new JsonException($"Unknown cover type: {s}")
            };
        }

        public override void Write(Utf8JsonWriter writer, CoverTypeEnum value, JsonSerializerOptions options)
        {
            var s = value switch
            {
                CoverTypeEnum.Hard => "hard",
                CoverTypeEnum.Soft => "soft",
                _ => string.Empty
            };
            writer.WriteStringValue(s);
        }
    }

    public class CoverColorConverter : JsonConverter<CoverColorEnum>
    {
        public override CoverColorEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString()?.ToLowerInvariant() ?? string.Empty;
            return s switch
            {
                "red" => CoverColorEnum.Red,
                "green" => CoverColorEnum.Green,
                "blue" => CoverColorEnum.Blue,
                "yellow" => CoverColorEnum.Yellow,
                _ => throw new JsonException($"Unknown cover color: {s}")
            };
        }

        public override void Write(Utf8JsonWriter writer, CoverColorEnum value, JsonSerializerOptions options)
        {
            var s = value switch
            {
                CoverColorEnum.Red => "red",
                CoverColorEnum.Green => "green",
                CoverColorEnum.Blue => "blue",
                CoverColorEnum.Yellow => "yellow",
                _ => string.Empty
            };
            writer.WriteStringValue(s);
        }
    }

    public class SpiralConverter : JsonConverter<SpiralEnum?>
    {
        public override SpiralEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            var s = reader.GetString()?.ToLowerInvariant() ?? string.Empty;
            return s switch
            {
                "plastic" => SpiralEnum.Plastic,
                "gluing" => SpiralEnum.Gluing,
                _ => null
            };
        }

        public override void Write(Utf8JsonWriter writer, SpiralEnum? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }
            var s = value switch
            {
                SpiralEnum.Plastic => "plastic",
                SpiralEnum.Gluing => "gluing",
                _ => string.Empty
            };
            writer.WriteStringValue(s);
        }
    }

    public enum DocumentTypeEnum
    {
        Tesis,
        Reporte,
        Examen,
        Otro
    }

    public class DocumentTypeConverter : JsonConverter<DocumentTypeEnum>
    {
        public override DocumentTypeEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString()?.ToLowerInvariant() ?? string.Empty;
            return s switch
            {
                "tesis" => DocumentTypeEnum.Tesis,
                "reporte" => DocumentTypeEnum.Reporte,
                "examen" => DocumentTypeEnum.Examen,
                "otro" => DocumentTypeEnum.Otro,
                _ => throw new JsonException($"Unknown document type: {s}")
            };
        }

        public override void Write(Utf8JsonWriter writer, DocumentTypeEnum value, JsonSerializerOptions options)
        {
            var s = value switch
            {
                DocumentTypeEnum.Tesis => "tesis",
                DocumentTypeEnum.Reporte => "reporte",
                DocumentTypeEnum.Examen => "examen",
                DocumentTypeEnum.Otro => "otro",
                _ => string.Empty
            };
            writer.WriteStringValue(s);
        }
    }

    public enum PaperTypeEnum
    {
        Bright,
        Mate,
        Satiny
    }

    public class PaperTypeConverter : JsonConverter<PaperTypeEnum>
    {
        public override PaperTypeEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString()?.ToLowerInvariant() ?? string.Empty;
            return s switch
            {
                "bright" => PaperTypeEnum.Bright,
                "mate" => PaperTypeEnum.Mate,
                "satiny" => PaperTypeEnum.Satiny,
                _ => throw new JsonException($"Unknown paper type: {s}")
            };
        }

        public override void Write(Utf8JsonWriter writer, PaperTypeEnum value, JsonSerializerOptions options)
        {
            var s = value switch
            {
                PaperTypeEnum.Bright => "bright",
                PaperTypeEnum.Mate => "mate",
                PaperTypeEnum.Satiny => "satiny",
                _ => string.Empty
            };
            writer.WriteStringValue(s);
        }
    }

    public enum SpiralTypeEnum
    {
        Stapled,
        Glued,
        Sewn
    }

    public class SpiralTypeConverter : JsonConverter<SpiralTypeEnum>
    {
        public override SpiralTypeEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString()?.ToLowerInvariant() ?? string.Empty;
            return s switch
            {
                "stapled" => SpiralTypeEnum.Stapled,
                "glued" => SpiralTypeEnum.Glued,
                "sewn" => SpiralTypeEnum.Sewn,
                _ => throw new JsonException($"Unknown spiral type: {s}")
            };
        }

        public override void Write(Utf8JsonWriter writer, SpiralTypeEnum value, JsonSerializerOptions options)
        {
            var s = value switch
            {
                SpiralTypeEnum.Stapled => "stapled",
                SpiralTypeEnum.Glued => "glued",
                SpiralTypeEnum.Sewn => "sewn",
                _ => string.Empty
            };
            writer.WriteStringValue(s);
        }
    }

    public enum FileStatusEnum
    {
        Active,
        Inactive
    }

    public class FileStatusConverter : JsonConverter<FileStatusEnum>
    {
        public override FileStatusEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString()?.ToLowerInvariant() ?? string.Empty;
            return s switch
            {
                "active" => FileStatusEnum.Active,
                "inactive" => FileStatusEnum.Inactive,
                _ => throw new JsonException($"Unknown file status: {s}")
            };
        }

        public override void Write(Utf8JsonWriter writer, FileStatusEnum value, JsonSerializerOptions options)
        {
            var s = value switch
            {
                FileStatusEnum.Active => "active",
                FileStatusEnum.Inactive => "inactive",
                _ => string.Empty
            };
            writer.WriteStringValue(s);
        }
    }

    public enum TransactionTypeEnum
    {
        Compra,
        Venta
    }

    public class TransactionTypeConverter : JsonConverter<TransactionTypeEnum>
    {
        public override TransactionTypeEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString()?.ToLowerInvariant() ?? string.Empty;
            return s switch
            {
                "compra" => TransactionTypeEnum.Compra,
                "venta" => TransactionTypeEnum.Venta,
                _ => throw new JsonException($"Unknown transaction type: {s}")
            };
        }

        public override void Write(Utf8JsonWriter writer, TransactionTypeEnum value, JsonSerializerOptions options)
        {
            var s = value switch
            {
                TransactionTypeEnum.Compra => "compra",
                TransactionTypeEnum.Venta => "venta",
                _ => string.Empty
            };
            writer.WriteStringValue(s);
        }
    }

    public enum TransactionStatusEnum
    {
        Pending,
        Completed
    }

    public class TransactionStatusConverter : JsonConverter<TransactionStatusEnum>
    {
        public override TransactionStatusEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString()?.ToLowerInvariant() ?? string.Empty;
            return s switch
            {
                "pending" => TransactionStatusEnum.Pending,
                "completed" => TransactionStatusEnum.Completed,
                _ => throw new JsonException($"Unknown transaction status: {s}")
            };
        }

        public override void Write(Utf8JsonWriter writer, TransactionStatusEnum value, JsonSerializerOptions options)
        {
            var s = value switch
            {
                TransactionStatusEnum.Pending => "pending",
                TransactionStatusEnum.Completed => "completed",
                _ => string.Empty
            };
            writer.WriteStringValue(s);
        }
    }

    public class DecimalConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetDecimal(out var d))
            {
                return d;
            }
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                {
                    return parsed;
                }
            }
            throw new JsonException("Unable to convert JSON token to decimal");
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }

}
