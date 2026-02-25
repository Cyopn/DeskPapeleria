using System;
using System.Text.Json.Serialization;

namespace DeskApp.Models
{
    public class SpecialServiceCreateRequest
    {
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
    }

    public class SpecialServiceUpdateRequest
    {
        [JsonPropertyName("delivery")]
        public DateTime Delivery { get; set; }

        [JsonPropertyName("observations")]
        public string? Observations { get; set; }
    }

    public class SpecialServiceResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("special_service")]
        public SpecialServiceData? SpecialService { get; set; }
    }
}
