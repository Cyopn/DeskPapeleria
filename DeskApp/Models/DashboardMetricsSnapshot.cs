using System;
using System.Collections.Generic;

namespace DeskApp.Models
{
    public sealed class DashboardMetricsSnapshot
    {
        public DateTime GeneratedAt { get; init; }
        public int TotalTransactions { get; init; }
        public decimal TotalAmount { get; init; }
        public decimal TicketAverage { get; init; }
        public List<(string Label, decimal Total)> SalesOverTime { get; init; } = new();
        public List<(string Label, decimal Total)> AccumulatedSales { get; init; } = new();
        public List<(string Label, decimal Total)> SalesByHour { get; init; } = new();
        public List<(string Label, decimal Total)> SalesByCategory { get; init; } = new();
        public List<(string Label, decimal Total)> PaymentMethods { get; init; } = new();
        public List<(string Product, int Quantity)> TopProducts { get; init; } = new();
        public List<(string Product, decimal Revenue)> IncomeByProduct { get; init; } = new();
        public List<(string Label, int Quantity, decimal Total)> SalesByCategoryDetailed { get; init; } = new();
        public List<(string Product, int Quantity, decimal Revenue)> IncomeByProductDetailed { get; init; } = new();
        public List<(string Label, int Count)> TransactionsByDay { get; init; } = new();
        public List<(string Label, int Count)> PurchaseSizeDistribution { get; init; } = new();
        public List<TransactionRow> TransactionsLog { get; init; } = new();

        public sealed class TransactionRow
        {
            public int Id { get; init; }
            public string Type { get; init; } = string.Empty;
            public string Product { get; init; } = string.Empty;
            public string Date { get; init; } = string.Empty;
            public string User { get; init; } = string.Empty;
            public decimal Total { get; init; }
            public string Status { get; init; } = string.Empty;
            public string PaymentMethod { get; init; } = string.Empty;
        }
    }
}
