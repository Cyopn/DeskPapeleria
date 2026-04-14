using System;
using DeskApp.Models;

namespace DeskApp.Helpers
{
    public static class EnumDisplayHelpers
    {
        public static string ToSpanish(CoverTypeEnum value) => value switch
        {
            CoverTypeEnum.Hard => "Tapa dura",
            CoverTypeEnum.Soft => "Tapa blanda",
            _ => value.ToString()
        };

        public static string ToSpanish(CoverColorEnum value) => value switch
        {
            CoverColorEnum.Red => "Rojo",
            CoverColorEnum.Green => "Verde",
            CoverColorEnum.Blue => "Azul",
            CoverColorEnum.Yellow => "Amarillo",
            _ => value.ToString()
        };

        public static string ToSpanish(SpiralEnum? value) => value switch
        {
            SpiralEnum.Plastic => "Plástico",
            SpiralEnum.Gluing => "Pegado",
            null => "-",
            _ => value.ToString()
        };

        public static string ToSpanish(SpiralTypeEnum value) => value switch
        {
            SpiralTypeEnum.Stapled => "Grapado",
            SpiralTypeEnum.Glued => "Pegado",
            SpiralTypeEnum.Sewn => "Cosido",
            _ => value.ToString()
        };

        public static string ToSpanish(DocumentTypeEnum value) => value switch
        {
            DocumentTypeEnum.Tesis => "Tesis",
            DocumentTypeEnum.Reporte => "Reporte",
            DocumentTypeEnum.Examen => "Examen",
            DocumentTypeEnum.Otro => "Otro",
            _ => value.ToString()
        };

        public static string ToSpanish(PaperTypeEnum value) => value switch
        {
            PaperTypeEnum.Bright => "Brillante",
            PaperTypeEnum.Mate => "Mate",
            PaperTypeEnum.Satiny => "Satín",
            _ => value.ToString()
        };

        public static string ToSpanishPhotoSize(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "-";
            switch (code.Trim().ToLowerInvariant())
            {
                case "ti": return "Tamaño infantil";
                case "tc": return "Tamaño carnet";
                case "tap": return "Tamaño álbum pequeño";
                default: return code;
            }
        }
    }
}
