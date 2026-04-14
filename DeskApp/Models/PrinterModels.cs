using System;
using System.Text.Json.Serialization;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace DeskApp.Models
{
    public enum PrinterConnectionType
    {
        Unknown = 0,
        Usb = 1,
        Network = 2,
        Installed = 3
    }

    public enum PrinterStatus
    {
        Available,
        Busy,
        Offline
    }

    public class PrinterData
    {
        [JsonPropertyName("id_printer")]
        public int IdPrinter { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        [JsonConverter(typeof(PrinterStatusConverter))]
        public PrinterStatus Status { get; set; } = PrinterStatus.Available;

        [JsonPropertyName("connection_type")]
        [JsonConverter(typeof(PrinterConnectionTypeConverter))]
        public PrinterConnectionType ConnectionType { get; set; } = PrinterConnectionType.Unknown;

        [JsonPropertyName("ip")]
        public string? IP { get; set; }

        [JsonPropertyName("port")]
        public int? Port { get; set; }

        [JsonPropertyName("port_name")]
        public string? PortName { get; set; }

        [JsonPropertyName("driver")]
        public string? Driver { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("serial_number")]
        public string? SerialNumber { get; set; }

        [JsonPropertyName("mac_address")]
        public string? MacAddress { get; set; }

        [JsonPropertyName("prints")]
        public ObservableCollection<PrintData>? Prints { get; set; } = new ObservableCollection<PrintData>();

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Name) ? base.ToString() : Name;
        }
    }

    public class PrinterConnectionTypeConverter : JsonConverter<PrinterConnectionType>
    {
        public override PrinterConnectionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString()?.Trim()?.ToLowerInvariant() ?? string.Empty;
                return s switch
                {
                    "usb" => PrinterConnectionType.Usb,
                    "network" => PrinterConnectionType.Network,
                    "installed" => PrinterConnectionType.Usb,
                    "unknown" => PrinterConnectionType.Usb,
                    _ => PrinterConnectionType.Usb,
                };
            }

            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var v))
            {
                return v switch
                {
                    1 => PrinterConnectionType.Usb,
                    2 => PrinterConnectionType.Network,
                    3 => PrinterConnectionType.Usb,
                    _ => PrinterConnectionType.Usb,
                };
            }

            return PrinterConnectionType.Usb;
        }

        public override void Write(Utf8JsonWriter writer, PrinterConnectionType value, JsonSerializerOptions options)
        {
            var s = value switch
            {
                PrinterConnectionType.Network => "network",
                _ => "usb",
            };
            writer.WriteStringValue(s);
        }
    }

    public class PrinterStatusConverter : JsonConverter<PrinterStatus>
    {
        public override PrinterStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString()?.Trim()?.ToLowerInvariant() ?? string.Empty;
                return s switch
                {
                    "available" => PrinterStatus.Available,
                    "busy" => PrinterStatus.Busy,
                    "offline" => PrinterStatus.Offline,
                    _ => PrinterStatus.Available,
                };
            }
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var v))
            {
                return v switch
                {
                    0 => PrinterStatus.Available,
                    1 => PrinterStatus.Busy,
                    2 => PrinterStatus.Offline,
                    _ => PrinterStatus.Available,
                };
            }

            return PrinterStatus.Available;
        }

        public override void Write(Utf8JsonWriter writer, PrinterStatus value, JsonSerializerOptions options)
        {
            var s = value switch
            {
                PrinterStatus.Available => "available",
                PrinterStatus.Busy => "busy",
                PrinterStatus.Offline => "offline",
                _ => "available",
            };
            writer.WriteStringValue(s);
        }
    }
}
