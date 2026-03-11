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

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("print")]
        public PrintData? Print { get; set; }

        [JsonPropertyName("data")]
        public SpecialServiceLinkData? Data { get; set; }

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

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
