namespace Velora.Application.Administration;

public sealed record AdminProductListItem(Guid Id, string Name, string Slug, string Category, decimal Price, bool IsFeatured, bool IsActive, bool IsArchived, int Stock, string ImageUrl);
public sealed record AdminCategoryListItem(Guid Id, string Name, string Slug, int ProductCount, bool IsActive, bool IsArchived);
public sealed record AdminCollectionListItem(Guid Id, string Name, string Slug, int ProductCount, bool IsFeatured, bool IsArchived, DateTime? PublishAtUtc);
public sealed record AdminLookup(Guid Id, string Name);
public sealed record AdminProductImage(Guid Id, string Url, string PublicId, string AltText, int DisplayOrder, bool IsPrimary, Guid? VariantId);

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

public sealed class AdminVariantModel
{
    public Guid? Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#000000";
    public string Size { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 5;
    public bool IsActive { get; set; } = true;
}

public sealed class AdminCategoryModel
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string SeoTitle { get; set; } = string.Empty;
    public string SeoDescription { get; set; } = string.Empty;
}

public sealed class AdminCollectionModel
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? PublishAtUtc { get; set; }
}
