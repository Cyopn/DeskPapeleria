using System;
using System.Text.Json.Serialization;

namespace DeskApp.Models
{
    public class QRCodeData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("id_qr")]
        public int IdQr { get; set; }

        [JsonPropertyName("id_transaction")]
        public int IdTransaction { get; set; }

        [JsonPropertyName("transaction")]
        public TransactionData? Transaction { get; set; }

        [JsonPropertyName("qr_data")]
        public string QrData { get; set; } = string.Empty;

        [JsonPropertyName("qr_image_base64")]
        public string QrImageBase64 { get; set; } = string.Empty;

        [JsonPropertyName("qr_info")]
        public string QrInfo { get; set; } = string.Empty;

        [JsonPropertyName("generated_at")]
        public DateTime GeneratedAt { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("scan_count")]
        public int ScanCount { get; set; }

        [JsonPropertyName("last_scanned_at")]
        public DateTime? LastScannedAt { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class QRCodeCreateRequest
    {
        [JsonPropertyName("id_transaction")]
        public int IdTransaction { get; set; }

        [JsonPropertyName("qr_data")]
        public string QrData { get; set; } = string.Empty;

        [JsonPropertyName("qr_image_base64")]
        public string QrImageBase64 { get; set; } = string.Empty;

        [JsonPropertyName("qr_info")]
        public string QrInfo { get; set; } = string.Empty;

        [JsonPropertyName("generated_at")]
        public DateTime? GeneratedAt { get; set; }

        [JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }
    }

    public class QRCodeUpdateRequest
    {
        [JsonPropertyName("qr_data")]
        public string? QrData { get; set; }

        [JsonPropertyName("qr_image_base64")]
        public string? QrImageBase64 { get; set; }

        [JsonPropertyName("qr_info")]
        public string? QrInfo { get; set; }

        [JsonPropertyName("generated_at")]
        public DateTime? GeneratedAt { get; set; }

        [JsonPropertyName("is_active")]
        public bool? IsActive { get; set; }

        [JsonPropertyName("scan_count")]
        public int? ScanCount { get; set; }

        [JsonPropertyName("last_scanned_at")]
        public DateTime? LastScannedAt { get; set; }
    }

    public class QRCodeResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("qr_code")]
        public QRCodeData? QRCode { get; set; }
    }
}
