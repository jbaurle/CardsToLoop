using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace InvoiceApp;

// The product the cards describe: an invoice with items, a discount and totals.
// Every control has an AutomationId so the agent can drive it over UI Automation.
// The app ships with gaps on purpose; each gap is a card in board/cards.
public partial class MainWindow : Window
{
    private readonly ObservableCollection<OrderItem> _items = new();

    public MainWindow()
    {
        InitializeComponent();

        ItemsGrid.ItemsSource = _items;

        // Two sample rows, so a run can read totals back before it types anything.
        _items.Add(new OrderItem { Description = "Widget", Quantity = 2, UnitPrice = 19.90m });
        _items.Add(new OrderItem { Description = "Gadget", Quantity = 1, UnitPrice = 49.00m });

        RecalculateTotals();
    }

    private void AddItemButton_Click(object sender, RoutedEventArgs e)
    {
        // NOTE: Quantity and price are not validated on purpose. TryParse leaves 0 when
        // the text is empty or not a number, and the row is added anyway. The card
        // validate-quantity-and-price asks for the check, and the agent run adds it.
        int.TryParse(NewQuantityTextBox.Text, out var quantity);
        decimal.TryParse(NewUnitPriceTextBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var unitPrice);

        _items.Add(new OrderItem
        {
            Description = NewDescriptionTextBox.Text,
            Quantity = quantity,
            UnitPrice = unitPrice,
        });

        NewDescriptionTextBox.Clear();
        NewQuantityTextBox.Text = "1";
        NewUnitPriceTextBox.Text = "0";

        RecalculateTotals();
    }

    private void DeleteItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (ItemsGrid.SelectedItem is OrderItem item)
        {
            var answer = MessageBox.Show(
                this,
                $"Delete \"{item.Description}\"?",
                "Delete item",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

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
        // XAML applies attributes in order. If Text and TextChanged of DiscountTextBox
        // ever swap places, the handler fires during InitializeComponent, before the
        // total fields below it exist.
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

        // NOTE: The discount is read but not applied on purpose; the total equals the subtotal.
        // The card total-ignores-the-discount describes the bug, and the agent run fixes it.
        TotalText.Text = subtotal.ToString("N2", CultureInfo.CurrentCulture);
    }
}
