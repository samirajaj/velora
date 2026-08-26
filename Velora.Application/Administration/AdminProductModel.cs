namespace Velora.Application.Administration;

public sealed class AdminProductModel
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? CollectionId { get; set; }
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
    public List<AdminVariantModel> Variants { get; set; } = [];
    public List<AdminProductImage> Images { get; set; } = [];
    public IReadOnlyList<AdminLookup> Categories { get; set; } = [];
    public IReadOnlyList<AdminLookup> Collections { get; set; } = [];
}
