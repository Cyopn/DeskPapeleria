using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeskApp.Models
{
    public class TransactionQrScanResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("scanned_at")]
        public DateTime? ScannedAt { get; set; }

        [JsonPropertyName("qr_data")]
        public JsonElement? QrData { get; set; }

        [JsonPropertyName("transaction")]
        public TransactionData? Transaction { get; set; }
    }
}
