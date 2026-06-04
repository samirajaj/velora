namespace Velora.Entities.Catalog;

public class ProductVariant
{
    public Guid Id { get; set; }

    public string Color { get; set; } = string.Empty;

    public string Size { get; set; } = string.Empty;

    public int StockQuantity { get; set; }

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;
}
