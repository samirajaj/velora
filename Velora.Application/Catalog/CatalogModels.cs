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
    IReadOnlyList<ProductOption> Options);

public sealed record CategorySummary(string Name, string Slug, string Description, string ImageUrl);

public sealed record CatalogRequest(
    string? Category = null,
    string? Search = null,
    string Sort = "featured",
    int Page = 1,
    int PageSize = 12);

public sealed record CatalogResult(
    IReadOnlyList<ProductCard> Items,
    IReadOnlyList<CategorySummary> Categories,
    int Page,
    int TotalPages,
    int TotalItems);
