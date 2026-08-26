namespace Velora.Domain.Catalog;

public sealed class InventoryReservation
{
    public Guid Id { get; set; }
    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public Guid? OrderId { get; set; }
    public int Quantity { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsReleased { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
