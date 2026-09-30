using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InvoiceApp;

public sealed class OrderItem : INotifyPropertyChanged
{
    private string _description = string.Empty;
    private int _quantity = 1;
    private decimal _unitPrice;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); }
    }

    public int Quantity
    {
        get => _quantity;
        set { _quantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(LineTotal)); }
    }

    public decimal UnitPrice
    {
        get => _unitPrice;
        set { _unitPrice = value; OnPropertyChanged(); OnPropertyChanged(nameof(LineTotal)); }
    }

    public decimal LineTotal => Quantity * UnitPrice;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
