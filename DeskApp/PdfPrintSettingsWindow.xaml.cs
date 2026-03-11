using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DeskApp.Models;

namespace DeskApp
{
    public partial class PdfPrintSettingsWindow : Window
    {
        public PdfPrintSettings? SelectedSettings { get; private set; }

        public PdfPrintSettingsWindow(IEnumerable<PrinterData> printers)
        {
            InitializeComponent();
            var list = printers?.ToList() ?? new List<PrinterData>();
            PrinterComboBox.ItemsSource = list;
            if (list.Count > 0)
            {
                PrinterComboBox.SelectedIndex = 0;
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (PrinterComboBox.SelectedItem is not PrinterData printer)
            {
                MessageBox.Show("Selecciona una impresora.", "Impresion", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(SetsTextBox.Text?.Trim(), out var sets) || sets <= 0)
            {
                MessageBox.Show("Juegos debe ser un numero mayor a 0.", "Impresion", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var colorTag = (ColorModeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "color";

            SelectedSettings = new PdfPrintSettings
            {
                PrinterId = printer.IdPrinter,
                PrinterName = printer.Name,
                ColorMode = colorTag,
                Range = string.IsNullOrWhiteSpace(RangeTextBox.Text) ? "all" : RangeTextBox.Text.Trim(),
                BothSides = BothSidesCheckBox.IsChecked == true,
                Copies = 1,
                Sets = sets
            };

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class PdfPrintSettings
    {
        public int PrinterId { get; set; }
        public string PrinterName { get; set; } = string.Empty;
        public string ColorMode { get; set; } = "color";
        public string Range { get; set; } = "all";
        public bool BothSides { get; set; }
        public int Copies { get; set; } = 1;
        public int Sets { get; set; } = 1;
    }
}
