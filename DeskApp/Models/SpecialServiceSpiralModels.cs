using System;
using System.Text.Json.Serialization;

namespace DeskApp.Models
{
    public class SpecialServiceSpiralData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("id_special_service_spiral")]
        public int IdSpecialServiceSpiral { get; set; }

        [JsonPropertyName("spiral_type")]
        [JsonConverter(typeof(DeskApp.SpiralTypeConverter))]
        public DeskApp.SpiralTypeEnum SpiralType { get; set; }

        [JsonPropertyName("special_service")]
        public SpecialService? SpecialService { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class SpecialServiceSpiralCreateRequest
    {
        [JsonPropertyName("id_special_service_spiral")]
        public int IdSpecialServiceSpiral { get; set; }

        [JsonPropertyName("spiral_type")]
        [JsonConverter(typeof(DeskApp.SpiralTypeConverter))]
        public DeskApp.SpiralTypeEnum SpiralType { get; set; }
    }

    public class SpecialServiceSpiralUpdateRequest
    {
        [JsonPropertyName("spiral_type")]
        [JsonConverter(typeof(DeskApp.SpiralTypeConverter))]
        public DeskApp.SpiralTypeEnum SpiralType { get; set; }
    }

    public class SpecialServiceSpiralResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("special_service_spiral")]
        public SpecialServiceSpiralData? SpecialServiceSpiral { get; set; }
    }
}
