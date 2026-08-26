namespace Velora.Domain.Catalog;

public sealed class ProductTag
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
