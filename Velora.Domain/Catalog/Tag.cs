namespace Velora.Domain.Catalog;

public sealed class Tag
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public ICollection<ProductTag> ProductTags { get; set; } = [];
}
