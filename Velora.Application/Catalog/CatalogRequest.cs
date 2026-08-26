namespace Velora.Application.Catalog;

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
