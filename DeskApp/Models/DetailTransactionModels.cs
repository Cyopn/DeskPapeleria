using System;
using System.Text.Json.Serialization;

namespace DeskApp.Models
{
    public class DetailTransactionData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("id_detail_transaction")]
        public int IdDetailTransaction { get; set; }

        [JsonPropertyName("id_transaction")]
        public int IdTransaction { get; set; }

        [JsonPropertyName("transaction")]
        public TransactionData? Transaction { get; set; }

        [JsonPropertyName("id_product")]
        public int IdProduct { get; set; }

        [JsonPropertyName("product")]
        public ProductData? Product { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class DetailTransactionCreateRequest
    {
        [JsonPropertyName("id_detail_transaction")]
        public int? IdDetailTransaction { get; set; }

        [JsonPropertyName("id_transaction")]
        public int IdTransaction { get; set; }

        [JsonPropertyName("id_product")]
        public int IdProduct { get; set; }

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }
    }

    public class DetailTransactionUpdateRequest
    {
        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }
    }

    public class DetailTransactionResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("detail_transaction")]
        public DetailTransactionData? DetailTransaction { get; set; }
    }
}
