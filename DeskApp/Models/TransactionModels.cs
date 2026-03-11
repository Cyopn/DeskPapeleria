using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeskApp.Models
{
    public class TransactionData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("id_transaction")]
        public int IdTransaction { get; set; }

        [JsonPropertyName("type")]
        [JsonConverter(typeof(DeskApp.TransactionTypeConverter))]
        public DeskApp.TransactionTypeEnum Type { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("id_user")]
        public int? IdUser { get; set; }

        [JsonPropertyName("user")]
        public UserData? User { get; set; }

        [JsonPropertyName("qr_code")]
        [JsonConverter(typeof(FlexibleQrCodeConverter))]
        public QRCodeData? QrCode { get; set; }

        [JsonPropertyName("details")]
        public List<DetailTransactionData>? Details { get; set; }

        [JsonPropertyName("total")]
        [JsonConverter(typeof(DeskApp.DecimalConverter))]
        public decimal Total { get; set; }

        [JsonPropertyName("status")]
        [JsonConverter(typeof(DeskApp.TransactionStatusConverter))]
        public DeskApp.TransactionStatusEnum Status { get; set; } = DeskApp.TransactionStatusEnum.Pending;

        [JsonPropertyName("payment_method")]
        public string? PaymentMethod { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class FlexibleQrCodeConverter : JsonConverter<QRCodeData?>
    {
        public override QRCodeData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var base64 = reader.GetString();
                return string.IsNullOrWhiteSpace(base64)
                    ? null
                    : new QRCodeData { QrImageBase64 = base64 };
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                return JsonSerializer.Deserialize<QRCodeData>(doc.RootElement.GetRawText(), options);
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, QRCodeData? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            JsonSerializer.Serialize(writer, value, options);
        }
    }

    public class TransactionCreateRequest
    {
        [JsonPropertyName("type")]
        [JsonConverter(typeof(DeskApp.TransactionTypeConverter))]
        public DeskApp.TransactionTypeEnum Type { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("id_user")]
        public int? IdUser { get; set; }

        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("status")]
        [JsonConverter(typeof(DeskApp.TransactionStatusConverter))]
        public DeskApp.TransactionStatusEnum Status { get; set; } = DeskApp.TransactionStatusEnum.Pending;

        [JsonPropertyName("payment_method")]
        public string? PaymentMethod { get; set; }
    }

    public class TransactionUpdateRequest
    {
        [JsonPropertyName("status")]
        [JsonConverter(typeof(DeskApp.TransactionStatusConverter))]
        public DeskApp.TransactionStatusEnum Status { get; set; }

        [JsonPropertyName("payment_method")]
        public string? PaymentMethod { get; set; }
    }

    public class TransactionResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("transaction")]
        public TransactionData? Transaction { get; set; }
    }
}
