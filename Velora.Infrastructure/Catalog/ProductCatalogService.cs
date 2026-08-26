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
        await context.Categories.AsNoTracking().Where(x => x.IsActive && !x.IsArchived).OrderBy(x => x.DisplayOrder)
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
        if (request.MinPrice.HasValue) query = query.Where(x => x.Price >= request.MinPrice.Value);
        if (request.MaxPrice.HasValue) query = query.Where(x => x.Price <= request.MaxPrice.Value);
        if (!string.IsNullOrWhiteSpace(request.Color)) query = query.Where(x => x.Variants.Any(v => v.IsActive && v.Color == request.Color));
        if (!string.IsNullOrWhiteSpace(request.Size)) query = query.Where(x => x.Variants.Any(v => v.IsActive && v.Size == request.Size));
        if (request.InStockOnly) query = query.Where(x => x.Variants.Any(v => v.IsActive && v.StockQuantity > 0));

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
        var colors = await context.ProductVariants.AsNoTracking().Where(x => x.IsActive && x.Product.IsActive && !x.Product.IsArchived).Select(x => x.Color).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
        var sizes = await context.ProductVariants.AsNoTracking().Where(x => x.IsActive && x.Product.IsActive && !x.Product.IsArchived).Select(x => x.Size).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
        return new CatalogResult(items, categories, page, Math.Max(1, (int)Math.Ceiling(total / (double)pageSize)), total, colors, sizes);
    }

    public async Task<ProductDetails?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        await context.Products.AsNoTracking().Where(x => x.IsActive && !x.IsArchived && (x.PublishAtUtc == null || x.PublishAtUtc <= DateTime.UtcNow) && x.Slug == slug)
            .Select(x => new ProductDetails(x.Id, x.Name, x.Slug, x.Description, x.Price, x.CompareAtPrice, x.ImageUrl,
                x.Category.Name, x.Category.Slug,
                x.Variants.Where(v => v.IsActive).OrderBy(v => v.Size).Select(v => new ProductOption(v.Id, v.Sku, v.Color, v.ColorHex, v.Size, v.StockQuantity)).ToList(),
                x.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder).Select(i => i.Url).ToList(), x.Material, x.CareInstructions, x.SeoTitle, x.SeoDescription,
                x.RelatedProducts.OrderBy(r => r.DisplayOrder).Where(r => r.RelatedProduct.IsActive && !r.RelatedProduct.IsArchived).Select(r => new ProductCard(r.RelatedProduct.Id, r.RelatedProduct.Name, r.RelatedProduct.Slug, r.RelatedProduct.Price, r.RelatedProduct.CompareAtPrice, r.RelatedProduct.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault() ?? r.RelatedProduct.ImageUrl, r.RelatedProduct.Category.Name, r.RelatedProduct.CreatedAtUtc >= DateTime.UtcNow.AddDays(-30))).Take(4).ToList()))
            .FirstOrDefaultAsync(cancellationToken);

    private IQueryable<Domain.Catalog.Product> ProductCards() => context.Products.AsNoTracking().Where(x => x.IsActive && !x.IsArchived && (x.PublishAtUtc == null || x.PublishAtUtc <= DateTime.UtcNow));

    private static System.Linq.Expressions.Expression<Func<Domain.Catalog.Product, ProductCard>> ToCard() =>
        x => new ProductCard(x.Id, x.Name, x.Slug, x.Price, x.CompareAtPrice, x.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault() ?? x.ImageUrl, x.Category.Name, x.CreatedAtUtc >= DateTime.UtcNow.AddDays(-30));
}
