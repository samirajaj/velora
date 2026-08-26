namespace Velora.Features.Cart;

public sealed record CartItem(Guid ProductId, Guid VariantId, string Slug, string Name, string ImageUrl, string Option, decimal UnitPrice, int Quantity)
{
    public decimal LineTotal => UnitPrice * Quantity;
}
