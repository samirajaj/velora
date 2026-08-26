using Velora.Domain.Catalog;

namespace Velora.Domain.Customers;

public sealed class CustomerCart
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<CustomerCartItem> Items { get; set; } = [];
}

public sealed class CustomerCartItem
{
    public Guid Id { get; set; }
    public Guid CustomerCartId { get; set; }
    public CustomerCart CustomerCart { get; set; } = null!;
    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
    public int Quantity { get; set; }
}

public sealed class WishlistItem
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
