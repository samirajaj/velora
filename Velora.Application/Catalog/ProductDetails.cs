namespace Velora.Application.Catalog;

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
