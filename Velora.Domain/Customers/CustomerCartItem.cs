using Velora.Domain.Catalog;

namespace Velora.Domain.Customers;

public sealed class CustomerCartItem
{
    public Guid Id { get; set; }
    public Guid CustomerCartId { get; set; }
    public CustomerCart CustomerCart { get; set; } = null!;
    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
    public int Quantity { get; set; }
}
