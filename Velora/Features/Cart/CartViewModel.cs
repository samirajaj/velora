namespace Velora.Features.Cart;

public sealed record CartViewModel(IReadOnlyList<CartItem> Items)
{
    public int ItemCount => Items.Sum(item => item.Quantity);
    public decimal Subtotal => Items.Sum(item => item.LineTotal);
}
