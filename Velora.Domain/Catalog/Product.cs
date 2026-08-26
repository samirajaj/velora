namespace Velora.Domain.Catalog;

public sealed class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? ImagePublicId { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public ICollection<ProductVariant> Variants { get; set; } = [];
}
