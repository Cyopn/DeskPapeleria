using System;
using System.Text.Json.Serialization;

namespace DeskApp.Models
{
    public class PrintData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("id_print")]
        public int IdPrint { get; set; }

        [JsonPropertyName("print_type")]
        public string PrintType { get; set; } = string.Empty;

        [JsonPropertyName("paper_type")]
        public string PaperType { get; set; } = string.Empty;

        [JsonPropertyName("paper_size")]
        public string PaperSize { get; set; } = string.Empty;

        [JsonPropertyName("range")]
        public string Range { get; set; } = string.Empty;

        [JsonPropertyName("both_sides")]
        public bool BothSides { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("observations")]
        public string? Observations { get; set; }

        [JsonPropertyName("status")]
        [JsonConverter(typeof(DeskApp.PrintStatusConverter))]
        public DeskApp.PrintStatusEnum Status { get; set; } = DeskApp.PrintStatusEnum.Pending;

        [JsonPropertyName("product")]
        public ProductData? Product { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class PrintCreateRequest
    {
        [JsonPropertyName("print_type")]
        public string PrintType { get; set; } = string.Empty;

        [JsonPropertyName("paper_type")]
        public string PaperType { get; set; } = string.Empty;

        [JsonPropertyName("paper_size")]
        public string PaperSize { get; set; } = string.Empty;

        [JsonPropertyName("range")]
        public string Range { get; set; } = string.Empty;

        [JsonPropertyName("both_sides")]
        public bool BothSides { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("observations")]
        public string? Observations { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("id_print")]
        public int? IdPrint { get; set; }
    }

    public class PrintUpdateRequest
    {
        [JsonPropertyName("print_type")]
        public string PrintType { get; set; } = string.Empty;

        [JsonPropertyName("paper_type")]
        public string PaperType { get; set; } = string.Empty;

        [JsonPropertyName("paper_size")]
        public string PaperSize { get; set; } = string.Empty;

        [JsonPropertyName("range")]
        public string Range { get; set; } = string.Empty;

        [JsonPropertyName("both_sides")]
        public bool BothSides { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("observations")]
        public string? Observations { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    public class PrintResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("print")]
        public PrintData? Print { get; set; }
    }
}
