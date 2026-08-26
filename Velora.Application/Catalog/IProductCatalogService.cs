namespace Velora.Application.Catalog;

public interface IProductCatalogService
{
    Task<IReadOnlyList<ProductCard>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategorySummary>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<CatalogResult> BrowseAsync(CatalogRequest request, CancellationToken cancellationToken = default);
    Task<ProductDetails?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
