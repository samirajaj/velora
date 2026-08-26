using Velora.Application.Media;

namespace Velora.Application.Administration;

public interface IAdminCatalogService
{
    Task<IReadOnlyList<AdminProductListItem>> GetProductsAsync(string? search, bool includeArchived, CancellationToken cancellationToken = default);
    Task<AdminProductModel> CreateProductModelAsync(CancellationToken cancellationToken = default);
    Task<AdminProductModel?> GetProductAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> SaveProductAsync(AdminProductModel model, Guid actorId, string ipAddress, CancellationToken cancellationToken = default);
    Task SetProductArchivedAsync(Guid id, bool archived, Guid actorId, string ipAddress, CancellationToken cancellationToken = default);
    Task SetProductFeaturedAsync(Guid id, bool featured, Guid actorId, string ipAddress, CancellationToken cancellationToken = default);
    Task AddImageAsync(Guid productId, MediaUploadResult upload, string altText, Guid? variantId, Guid actorId, string ipAddress, CancellationToken cancellationToken = default);
    Task<AdminProductImage?> GetImageAsync(Guid imageId, CancellationToken cancellationToken = default);
    Task DeleteImageRecordAsync(Guid imageId, Guid actorId, string ipAddress, CancellationToken cancellationToken = default);
    Task SetPrimaryImageAsync(Guid productId, Guid imageId, Guid actorId, string ipAddress, CancellationToken cancellationToken = default);
    Task ReorderImagesAsync(Guid productId, IReadOnlyList<Guid> imageIds, Guid actorId, string ipAddress, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminCategoryListItem>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<AdminCategoryModel?> GetCategoryAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> SaveCategoryAsync(AdminCategoryModel model, Guid actorId, string ipAddress, CancellationToken cancellationToken = default);
    Task SetCategoryArchivedAsync(Guid id, bool archived, Guid actorId, string ipAddress, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminCollectionListItem>> GetCollectionsAsync(CancellationToken cancellationToken = default);
    Task<AdminCollectionModel?> GetCollectionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> SaveCollectionAsync(AdminCollectionModel model, Guid actorId, string ipAddress, CancellationToken cancellationToken = default);
    Task SetCollectionArchivedAsync(Guid id, bool archived, Guid actorId, string ipAddress, CancellationToken cancellationToken = default);
}
