namespace Velora.Domain.Catalog;

public sealed class ProductRelation
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid RelatedProductId { get; set; }
    public Product RelatedProduct { get; set; } = null!;
    public int DisplayOrder { get; set; }
}
