namespace Velora.Features.Cart;

public sealed record CartItem(
    Guid ProductId,
    Guid VariantId,
    string Slug,
    string Name,
    string ImageUrl,
    string Option,
    decimal UnitPrice,
    int Quantity)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed record CartViewModel(IReadOnlyList<CartItem> Items)
{
    public int ItemCount => Items.Sum(x => x.Quantity);
    public decimal Subtotal => Items.Sum(x => x.LineTotal);
}
