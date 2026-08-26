namespace Velora.Domain.Catalog;

public sealed class ProductVariant
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#000000";
    public string Size { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
