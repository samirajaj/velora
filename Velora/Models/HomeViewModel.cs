using Velora.Application.Catalog;

namespace Velora.Models;

public sealed record HomeViewModel(
    IReadOnlyList<ProductCard> FeaturedProducts,
    IReadOnlyList<CategorySummary> Categories);
