using System;
using System.Text.Json.Serialization;

namespace DeskApp.Models
{
    public class SpecialServiceData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("id_special_service_data")]
        public int IdSpecialServiceData { get; set; }

        [JsonPropertyName("id_special_service")]
        public int? IdSpecialService { get; set; }

        [JsonPropertyName("id_print")]
        public int IdPrint { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("mode")]
        public string? Mode { get; set; }

        [JsonPropertyName("delivery")]
        public DateTime? Delivery { get; set; }

        [JsonPropertyName("observations")]
        public string? Observations { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("print")]
        public PrintData? Print { get; set; }

        [JsonPropertyName("data")]
        public SpecialServiceLinkData? Data { get; set; }

        [JsonPropertyName("bound")]
        public SpecialServiceBoundData? Bound { get; set; }

        [JsonPropertyName("spiral")]
        public SpecialServiceSpiralData? Spiral { get; set; }

        [JsonPropertyName("document")]
        public SpecialServiceDocumentData? Document { get; set; }

        [JsonPropertyName("photo")]
        public SpecialServicePhotoData? Photo { get; set; }

        [JsonPropertyName("special_service")]
        public SpecialService? SpecialService { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class SpecialServiceLinkData
    {
        [JsonPropertyName("id_print")]
        public int IdPrint { get; set; }
    }

    public class SpecialServiceDataCreateRequest
    {
        [JsonPropertyName("id_special_service_data")]
        public int IdSpecialServiceData { get; set; }

        [JsonPropertyName("id_print")]
        public int IdPrint { get; set; }
    }

    public class SpecialServiceDataUpdateRequest
    {
        [JsonPropertyName("id_print")]
        public int IdPrint { get; set; }
    }

    public class SpecialServiceDataResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("special_service_data")]
        public SpecialServiceData? SpecialServiceData { get; set; }
    }

    public class SpecialService
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("id_special_service")]
        public int IdSpecialService { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("mode")]
        public string? Mode { get; set; }

        [JsonPropertyName("delivery")]
        public DateTime? Delivery { get; set; }

        [JsonPropertyName("observations")]
        public string? Observations { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("bound")]
        public SpecialServiceBoundData? Bound { get; set; }

        [JsonPropertyName("spiral")]
        public SpecialServiceSpiralData? Spiral { get; set; }

        [JsonPropertyName("document")]
        public SpecialServiceDocumentData? Document { get; set; }

        [JsonPropertyName("photo")]
        public SpecialServicePhotoData? Photo { get; set; }
    }
}
