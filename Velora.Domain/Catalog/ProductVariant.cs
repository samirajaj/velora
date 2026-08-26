namespace Velora.Domain.Catalog;

public sealed class ProductVariant
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#000000";
    public string Size { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 5;
    public bool IsActive { get; set; } = true;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public ICollection<ProductImage> Images { get; set; } = [];
    public ICollection<InventoryReservation> Reservations { get; set; } = [];
}
