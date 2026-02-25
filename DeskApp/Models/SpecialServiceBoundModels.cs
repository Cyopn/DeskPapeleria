using System;
using System.Text.Json.Serialization;

namespace DeskApp.Models
{
    public class SpecialServiceBoundData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("id_special_service_bound")]
        public int IdSpecialServiceBound { get; set; }

        [JsonPropertyName("cover_type")]
        [JsonConverter(typeof(DeskApp.CoverTypeConverter))]
        public DeskApp.CoverTypeEnum CoverType { get; set; }

        [JsonPropertyName("cover_color")]
        [JsonConverter(typeof(DeskApp.CoverColorConverter))]
        public DeskApp.CoverColorEnum CoverColor { get; set; }

        [JsonPropertyName("spiral")]
        [JsonConverter(typeof(DeskApp.SpiralConverter))]
        public DeskApp.SpiralEnum? Spiral { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class SpecialServiceBoundCreateRequest
    {
        [JsonPropertyName("id_special_service_bound")]
        public int IdSpecialServiceBound { get; set; }

        [JsonPropertyName("cover_type")]
        [JsonConverter(typeof(DeskApp.CoverTypeConverter))]
        public DeskApp.CoverTypeEnum CoverType { get; set; }

        [JsonPropertyName("cover_color")]
        [JsonConverter(typeof(DeskApp.CoverColorConverter))]
        public DeskApp.CoverColorEnum CoverColor { get; set; }

        [JsonPropertyName("spiral")]
        [JsonConverter(typeof(DeskApp.SpiralConverter))]
        public DeskApp.SpiralEnum? Spiral { get; set; }
    }

    public class SpecialServiceBoundUpdateRequest
    {
        [JsonPropertyName("cover_type")]
        [JsonConverter(typeof(DeskApp.CoverTypeConverter))]
        public DeskApp.CoverTypeEnum CoverType { get; set; }

        [JsonPropertyName("cover_color")]
        [JsonConverter(typeof(DeskApp.CoverColorConverter))]
        public DeskApp.CoverColorEnum CoverColor { get; set; }

        [JsonPropertyName("spiral")]
        [JsonConverter(typeof(DeskApp.SpiralConverter))]
        public DeskApp.SpiralEnum? Spiral { get; set; }
    }

    public class SpecialServiceBoundResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("special_service_bound")]
        public SpecialServiceBoundData? SpecialServiceBound { get; set; }
    }
}
