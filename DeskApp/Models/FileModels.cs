using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeskApp.Models
{
    public enum FileType
    {
        Image,
        Document,
        Other
    }

    public enum FileStatus
    {
        Active,
        Inactive
    }

    public class FileTypeConverter : JsonConverter<FileType>
    {
        public override FileType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString() ?? string.Empty;
                if (string.Equals(s, "image", StringComparison.OrdinalIgnoreCase)) return FileType.Image;
                if (string.Equals(s, "document", StringComparison.OrdinalIgnoreCase)) return FileType.Document;
                return FileType.Other;
            }
            return FileType.Other;
        }

        public override void Write(Utf8JsonWriter writer, FileType value, JsonSerializerOptions options)
        {
            var s = value.ToString();
            writer.WriteStringValue(s.ToLowerInvariant());
        }
    }

    public class FileStatusConverter : JsonConverter<FileStatus>
    {
        public override FileStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString() ?? string.Empty;
                if (string.Equals(s, "active", StringComparison.OrdinalIgnoreCase)) return FileStatus.Active;
                if (string.Equals(s, "inactive", StringComparison.OrdinalIgnoreCase)) return FileStatus.Inactive;
                return FileStatus.Active;
            }
            return FileStatus.Active;
        }

        public override void Write(Utf8JsonWriter writer, FileStatus value, JsonSerializerOptions options)
        {
            var s = value.ToString();
            writer.WriteStringValue(s.ToLowerInvariant());
        }
    }

    public class FileData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("id_file")]
        public int IdFile { get; set; }

        [JsonPropertyName("id_user")]
        public int IdUser { get; set; }

        [JsonPropertyName("filename")]
        public string? Filename { get; set; }

        [JsonPropertyName("status")]
        [JsonConverter(typeof(FileStatusConverter))]
        public FileStatus Status { get; set; }

        [JsonPropertyName("type")]
        [JsonConverter(typeof(FileTypeConverter))]
        public FileType Type { get; set; }

        [JsonPropertyName("filehash")]
        public string? FileHash { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class FileCreateRequest
    {
        [JsonPropertyName("id_user")]
        public int IdUser { get; set; }

        [JsonPropertyName("filename")]
        public string? Filename { get; set; }

        [JsonPropertyName("type")]
        [JsonConverter(typeof(FileTypeConverter))]
        public FileType Type { get; set; }

        [JsonPropertyName("filehash")]
        public string? FileHash { get; set; }
    }

    public class FileUpdateRequest
    {
        [JsonPropertyName("filename")]
        public string? Filename { get; set; }

        [JsonPropertyName("status")]
        [JsonConverter(typeof(FileStatusConverter))]
        public FileStatus Status { get; set; }

        [JsonPropertyName("type")]
        [JsonConverter(typeof(FileTypeConverter))]
        public FileType Type { get; set; }

        [JsonPropertyName("filehash")]
        public string? FileHash { get; set; }
    }
}
