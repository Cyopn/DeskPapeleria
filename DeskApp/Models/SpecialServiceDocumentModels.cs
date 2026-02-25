using System;
using System.Text.Json.Serialization;

namespace DeskApp.Models
{
    public class SpecialServiceDocumentData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("id_special_service_document")]
        public int IdSpecialServiceDocument { get; set; }

        [JsonPropertyName("document_type")]
        [JsonConverter(typeof(DeskApp.DocumentTypeConverter))]
        public DeskApp.DocumentTypeEnum DocumentType { get; set; }

        [JsonPropertyName("special_service")]
        public SpecialService? SpecialService { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class SpecialServiceDocumentCreateRequest
    {
        [JsonPropertyName("id_special_service_document")]
        public int IdSpecialServiceDocument { get; set; }

        [JsonPropertyName("document_type")]
        [JsonConverter(typeof(DeskApp.DocumentTypeConverter))]
        public DeskApp.DocumentTypeEnum DocumentType { get; set; }
    }

    public class SpecialServiceDocumentUpdateRequest
    {
        [JsonPropertyName("document_type")]
        [JsonConverter(typeof(DeskApp.DocumentTypeConverter))]
        public DeskApp.DocumentTypeEnum DocumentType { get; set; }
    }

    public class SpecialServiceDocumentResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("special_service_document")]
        public SpecialServiceDocumentData? SpecialServiceDocument { get; set; }
    }
}
