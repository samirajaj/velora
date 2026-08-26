namespace Velora.Domain.Customers;

public sealed class CustomerCart
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<CustomerCartItem> Items { get; set; } = [];
}
