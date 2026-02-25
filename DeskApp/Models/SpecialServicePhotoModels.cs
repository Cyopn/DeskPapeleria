using System;
using System.Text.Json.Serialization;

namespace DeskApp.Models
{
    public class SpecialServicePhotoData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("id_special_service_photo")]
        public int IdSpecialServicePhoto { get; set; }

        [JsonPropertyName("photo_size")]
        public string PhotoSize { get; set; } = string.Empty;

        [JsonPropertyName("paper_type")]
        [JsonConverter(typeof(DeskApp.PaperTypeConverter))]
        public DeskApp.PaperTypeEnum PaperType { get; set; }

        [JsonPropertyName("special_service")]
        public SpecialService? SpecialService { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class SpecialServicePhotoCreateRequest
    {
        [JsonPropertyName("id_special_service_photo")]
        public int IdSpecialServicePhoto { get; set; }

        [JsonPropertyName("photo_size")]
        public string PhotoSize { get; set; } = string.Empty;

        [JsonPropertyName("paper_type")]
        [JsonConverter(typeof(DeskApp.PaperTypeConverter))]
        public DeskApp.PaperTypeEnum PaperType { get; set; }
    }

    public class SpecialServicePhotoUpdateRequest
    {
        [JsonPropertyName("photo_size")]
        public string PhotoSize { get; set; } = string.Empty;

        [JsonPropertyName("paper_type")]
        [JsonConverter(typeof(DeskApp.PaperTypeConverter))]
        public DeskApp.PaperTypeEnum PaperType { get; set; }
    }

    public class SpecialServicePhotoResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("special_service_photo")]
        public SpecialServicePhotoData? SpecialServicePhoto { get; set; }
    }
}
