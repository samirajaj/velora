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
    public string Material { get; set; } = string.Empty;
    public string CareInstructions { get; set; } = string.Empty;
    public string SeoTitle { get; set; } = string.Empty;
    public string SeoDescription { get; set; } = string.Empty;
    public int? WeightGrams { get; set; }
    public decimal? ShippingLengthCm { get; set; }
    public decimal? ShippingWidthCm { get; set; }
    public decimal? ShippingHeightCm { get; set; }
    public DateTime? PublishAtUtc { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public Guid? CollectionId { get; set; }
    public Collection? Collection { get; set; }
    public ICollection<ProductVariant> Variants { get; set; } = [];
    public ICollection<ProductImage> Images { get; set; } = [];
    public ICollection<ProductTag> ProductTags { get; set; } = [];
    public ICollection<ProductRelation> RelatedProducts { get; set; } = [];
}
