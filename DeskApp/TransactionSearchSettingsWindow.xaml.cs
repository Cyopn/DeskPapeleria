using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DeskApp
{
    public partial class TransactionSearchSettingsWindow : Window
    {
        public string SelectedSearchField { get; private set; } = "all";

        public TransactionSearchSettingsWindow(string currentSelection)
        {
            InitializeComponent();

            var selected = SearchFieldComboBox.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(i => string.Equals(i.Tag?.ToString(), currentSelection, StringComparison.OrdinalIgnoreCase));

            SearchFieldComboBox.SelectedItem = selected ?? SearchFieldComboBox.Items[0];
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (SearchFieldComboBox.SelectedItem is ComboBoxItem item)
            {
                SelectedSearchField = item.Tag?.ToString() ?? "all";
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
