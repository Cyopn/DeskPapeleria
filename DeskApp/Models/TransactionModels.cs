using System;
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
        public QRCodeData? QrCode { get; set; }

        [JsonPropertyName("total")]
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
