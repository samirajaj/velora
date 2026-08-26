using Microsoft.EntityFrameworkCore;
using Velora.Application.Catalog;
using Velora.Infrastructure.Persistence;

namespace Velora.Infrastructure.Catalog;

internal sealed class ProductCatalogService(ApplicationDbContext context) : IProductCatalogService
{
    public async Task<IReadOnlyList<ProductCard>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default) =>
        await ProductCards()
            .Where(x => x.IsFeatured)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(Math.Clamp(count, 1, 12))
            .Select(ToCard())
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CategorySummary>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        await context.Categories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayOrder)
            .Select(x => new CategorySummary(x.Name, x.Slug, x.Description, x.ImageUrl)).ToListAsync(cancellationToken);

    public async Task<CatalogResult> BrowseAsync(CatalogRequest request, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 48);
        var query = ProductCards();

        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(x => x.Category.Slug == request.Category);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(term) || x.Description.Contains(term));
        }

        query = request.Sort switch
        {
            "price-asc" => query.OrderBy(x => x.Price),
            "price-desc" => query.OrderByDescending(x => x.Price),
            "newest" => query.OrderByDescending(x => x.CreatedAtUtc),
            _ => query.OrderByDescending(x => x.IsFeatured).ThenByDescending(x => x.CreatedAtUtc)
        };

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(ToCard()).ToListAsync(cancellationToken);
        var categories = await GetCategoriesAsync(cancellationToken);
        return new CatalogResult(items, categories, page, Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)), total);
    }

    public async Task<ProductDetails?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        await context.Products.AsNoTracking().Where(x => x.IsActive && x.Slug == slug)
            .Select(x => new ProductDetails(x.Id, x.Name, x.Slug, x.Description, x.Price, x.CompareAtPrice, x.ImageUrl,
                x.Category.Name, x.Category.Slug,
                x.Variants.OrderBy(v => v.Size).Select(v => new ProductOption(v.Id, v.Sku, v.Color, v.ColorHex, v.Size, v.StockQuantity)).ToList()))
            .FirstOrDefaultAsync(cancellationToken);

    private IQueryable<Domain.Catalog.Product> ProductCards() => context.Products.AsNoTracking().Where(x => x.IsActive);

    private static System.Linq.Expressions.Expression<Func<Domain.Catalog.Product, ProductCard>> ToCard() =>
        x => new ProductCard(x.Id, x.Name, x.Slug, x.Price, x.CompareAtPrice, x.ImageUrl, x.Category.Name, x.CreatedAtUtc >= DateTime.UtcNow.AddDays(-30));
}
