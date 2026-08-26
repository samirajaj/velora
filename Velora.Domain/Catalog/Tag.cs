namespace Velora.Domain.Catalog;

public sealed class Tag
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public ICollection<ProductTag> ProductTags { get; set; } = [];
}

public sealed class ProductTag
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
