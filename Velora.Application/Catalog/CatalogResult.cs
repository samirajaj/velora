namespace Velora.Application.Catalog;

public sealed record CatalogResult(
    IReadOnlyList<ProductCard> Items,
    IReadOnlyList<CategorySummary> Categories,
    int Page,
    int TotalPages,
    int TotalItems,
    IReadOnlyList<string> AvailableColors,
    IReadOnlyList<string> AvailableSizes);
