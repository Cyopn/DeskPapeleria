using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace DeskApp.Models
{
    public class ProductData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("id_product")]
        public int IdProduct { get; set; }

        [JsonPropertyName("type")]
        [JsonConverter(typeof(DeskApp.ProductTypeConverter))]
        public DeskApp.ProductTypeEnum Type { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("price")]
        [JsonConverter(typeof(DeskApp.DecimalConverter))]
        public decimal Price { get; set; }

        [JsonPropertyName("id_file")]
        public int? IdFile { get; set; }

        [JsonPropertyName("id_files")]
        public List<int>? IdFiles { get; set; }

        [JsonPropertyName("files")]
        public List<FileData>? Files { get; set; }
        
        [JsonPropertyName("item")]
        public ItemData? Item { get; set; }

        [JsonPropertyName("print")]
        public PrintData? Print { get; set; }
        
        [JsonPropertyName("special_service")]
        public SpecialServiceData? SpecialService { get; set; }
        
        [JsonPropertyName("file")]
        public FileData? File { get; set; }
        
        [JsonPropertyName("detail_transactions")]
        public List<DetailTransactionData>? DetailTransactions { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class ProductCreateRequest
    {
        [JsonPropertyName("type")]
        [JsonConverter(typeof(DeskApp.ProductTypeConverter))]
        public DeskApp.ProductTypeEnum Type { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("price")]
        [JsonConverter(typeof(DeskApp.DecimalConverter))]
        public decimal Price { get; set; }

        [JsonPropertyName("id_file")]
        public int? IdFile { get; set; }

        [JsonPropertyName("id_files")]
        public List<int>? IdFiles { get; set; }

        [JsonPropertyName("category")]
        public ItemCategory Category { get; set; }
        
        [JsonPropertyName("item")]
        public ItemCreateRequest? Item { get; set; }
    }

    public class ProductUpdateRequest
    {
        [JsonPropertyName("type")]
        [JsonConverter(typeof(DeskApp.ProductTypeConverter))]
        public DeskApp.ProductTypeEnum Type { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("price")]
        [JsonConverter(typeof(DeskApp.DecimalConverter))]
        public decimal Price { get; set; }

        [JsonPropertyName("id_file")]
        public int? IdFile { get; set; }

        [JsonPropertyName("id_files")]
        public List<int>? IdFiles { get; set; }

        [JsonPropertyName("category")]
        public ItemCategory? Category { get; set; }
        
        [JsonPropertyName("item")]
        public ItemUpdateRequest? Item { get; set; }

        [JsonPropertyName("file")]
        public FileUpdateRequest? File { get; set; }
    }

    public class ProductResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("product")]
        public ProductData? Product { get; set; }
    }

}
