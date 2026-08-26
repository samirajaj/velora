namespace Velora.Application.Catalog;

public sealed record ProductCard(
    Guid Id,
    string Name,
    string Slug,
    decimal Price,
    decimal? CompareAtPrice,
    string ImageUrl,
    string CategoryName,
    bool IsNew);

public sealed record ProductOption(
    Guid Id,
    string Sku,
    string Color,
    string ColorHex,
    string Size,
    int StockQuantity);

public sealed record ProductDetails(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    decimal? CompareAtPrice,
    string ImageUrl,
    string CategoryName,
    string CategorySlug,
    IReadOnlyList<ProductOption> Options,
    IReadOnlyList<string> Images,
    string Material,
    string CareInstructions,
    string SeoTitle,
    string SeoDescription,
    IReadOnlyList<ProductCard> RelatedProducts);

public sealed record CategorySummary(string Name, string Slug, string Description, string ImageUrl);

public sealed record CatalogRequest(
    string? Category = null,
    string? Search = null,
    string Sort = "featured",
    int Page = 1,
    int PageSize = 12,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Color = null,
    string? Size = null,
    bool InStockOnly = false);

public sealed record CatalogResult(
    IReadOnlyList<ProductCard> Items,
    IReadOnlyList<CategorySummary> Categories,
    int Page,
    int TotalPages,
    int TotalItems,
    IReadOnlyList<string> AvailableColors,
    IReadOnlyList<string> AvailableSizes);
