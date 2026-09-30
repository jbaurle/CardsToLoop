using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace InvoiceApp;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<OrderItem> _items = new();

    public MainWindow()
    {
        InitializeComponent();
        ItemsGrid.ItemsSource = _items;
        _items.Add(new OrderItem { Description = "Widget", Quantity = 2, UnitPrice = 19.90m });
        _items.Add(new OrderItem { Description = "Gadget", Quantity = 1, UnitPrice = 49.00m });
        RecalculateTotals();
    }

    private void AddItemButton_Click(object sender, RoutedEventArgs e)
    {
        // NOTE: Quantity and price are parsed without validation on purpose - a card
        // in board/cards asks for input validation, and the agent run adds it.
        var quantity = int.TryParse(NewQuantityTextBox.Text, out var q) ? q : 0;
        var price = decimal.TryParse(NewUnitPriceTextBox.Text, NumberStyles.Number,
            CultureInfo.CurrentCulture, out var p) ? p : 0m;
        _items.Add(new OrderItem
        {
            Description = NewDescriptionTextBox.Text,
            Quantity = quantity,
            UnitPrice = price,
        });
        NewDescriptionTextBox.Clear();
        NewQuantityTextBox.Text = "1";
        NewUnitPriceTextBox.Text = "0";
        RecalculateTotals();
    }

    private void DeleteItemButton_Click(object sender, RoutedEventArgs e)
    {
        // NOTE: Deletes without asking on purpose - a card asks for a confirmation
        // dialog, and the agent run adds it.
        if (ItemsGrid.SelectedItem is OrderItem item)
        {
            _items.Remove(item);
            RecalculateTotals();
        }
    }

    private void DiscountTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        if (SubtotalText is null || TotalText is null)
        {
            return;
        }
        var subtotal = 0m;
        foreach (var item in _items)
        {
            subtotal += item.LineTotal;
        }
        SubtotalText.Text = subtotal.ToString("N2", CultureInfo.CurrentCulture);
        // NOTE: The discount is read but never applied on purpose - this is the bug the
        // first card describes, and the agent run fixes it.
        TotalText.Text = subtotal.ToString("N2", CultureInfo.CurrentCulture);
    }
}
