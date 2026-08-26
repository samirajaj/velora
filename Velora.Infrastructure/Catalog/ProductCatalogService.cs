using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Velora.Application.Catalog;
using Velora.Domain.Catalog;
using Velora.Infrastructure.Persistence;

namespace Velora.Infrastructure.Catalog;

internal sealed class ProductCatalogService(ApplicationDbContext context) : IProductCatalogService
{
    public async Task<IReadOnlyList<ProductCard>> GetFeaturedAsync(
        int count,
        CancellationToken cancellationToken = default) =>
        await PublishedProducts()
            .Where(product => product.IsFeatured)
            .OrderByDescending(product => product.CreatedAtUtc)
            .Take(Math.Clamp(count, 1, 12))
            .Select(ProductCardProjection)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CategorySummary>> GetCategoriesAsync(
        CancellationToken cancellationToken = default) =>
        await context.Categories
            .AsNoTracking()
            .Where(category => category.IsActive && !category.IsArchived)
            .OrderBy(category => category.DisplayOrder)
            .Select(category => new CategorySummary(
                category.Name,
                category.Slug,
                category.Description,
                category.ImageUrl))
            .ToListAsync(cancellationToken);

    public async Task<CatalogResult> BrowseAsync(
        CatalogRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 48);
        var query = ApplySort(ApplyFilters(PublishedProducts(), request), request.Sort);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ProductCardProjection)
            .ToListAsync(cancellationToken);
        var categories = await GetCategoriesAsync(cancellationToken);
        var colors = await GetAvailableColorsAsync(cancellationToken);
        var sizes = await GetAvailableSizesAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));

        return new CatalogResult(items, categories, page, totalPages, total, colors, sizes);
    }

    public Task<ProductDetails?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default) =>
        PublishedProducts()
            .Where(product => product.Slug == slug)
            .Select(ProductDetailsProjection)
            .FirstOrDefaultAsync(cancellationToken);

    private static IQueryable<Product> ApplyFilters(
        IQueryable<Product> query,
        CatalogRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(product => product.Category.Slug == request.Category);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(product =>
                product.Name.Contains(term) || product.Description.Contains(term));
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(product => product.Price >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(product => product.Price <= request.MaxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Color))
        {
            query = query.Where(product => product.Variants.Any(variant =>
                variant.IsActive && variant.Color == request.Color));
        }

        if (!string.IsNullOrWhiteSpace(request.Size))
        {
            query = query.Where(product => product.Variants.Any(variant =>
                variant.IsActive && variant.Size == request.Size));
        }

        if (request.InStockOnly)
        {
            query = query.Where(product => product.Variants.Any(variant =>
                variant.IsActive && variant.StockQuantity > 0));
        }

        return query;
    }

    private static IQueryable<Product> ApplySort(IQueryable<Product> query, string? sort) =>
        sort switch
        {
            "price-asc" => query.OrderBy(product => product.Price),
            "price-desc" => query.OrderByDescending(product => product.Price),
            "newest" => query.OrderByDescending(product => product.CreatedAtUtc),
            _ => query
                .OrderByDescending(product => product.IsFeatured)
                .ThenByDescending(product => product.CreatedAtUtc)
        };

    private Task<List<string>> GetAvailableColorsAsync(CancellationToken cancellationToken) =>
        AvailableVariants()
            .Select(variant => variant.Color)
            .Distinct()
            .OrderBy(color => color)
            .ToListAsync(cancellationToken);

    private Task<List<string>> GetAvailableSizesAsync(CancellationToken cancellationToken) =>
        AvailableVariants()
            .Select(variant => variant.Size)
            .Distinct()
            .OrderBy(size => size)
            .ToListAsync(cancellationToken);

    private IQueryable<ProductVariant> AvailableVariants() =>
        context.ProductVariants
            .AsNoTracking()
            .Where(variant =>
                variant.IsActive &&
                variant.Product.IsActive &&
                !variant.Product.IsArchived);

    private IQueryable<Product> PublishedProducts()
    {
        var now = DateTime.UtcNow;
        return context.Products
            .AsNoTracking()
            .Where(product =>
                product.IsActive &&
                !product.IsArchived &&
                (product.PublishAtUtc == null || product.PublishAtUtc <= now));
    }

    private static Expression<Func<Product, ProductCard>> ProductCardProjection =>
        product => new ProductCard(
            product.Id,
            product.Name,
            product.Slug,
            product.Price,
            product.CompareAtPrice,
            product.Images
                .OrderByDescending(image => image.IsPrimary)
                .ThenBy(image => image.DisplayOrder)
                .Select(image => image.Url)
                .FirstOrDefault() ?? product.ImageUrl,
            product.Category.Name,
            product.CreatedAtUtc >= DateTime.UtcNow.AddDays(-30));

    private static Expression<Func<Product, ProductDetails>> ProductDetailsProjection =>
        product => new ProductDetails(
            product.Id,
            product.Name,
            product.Slug,
            product.Description,
            product.Price,
            product.CompareAtPrice,
            product.ImageUrl,
            product.Category.Name,
            product.Category.Slug,
            product.Variants
                .Where(variant => variant.IsActive)
                .OrderBy(variant => variant.Size)
                .Select(variant => new ProductOption(
                    variant.Id,
                    variant.Sku,
                    variant.Color,
                    variant.ColorHex,
                    variant.Size,
                    variant.StockQuantity))
                .ToList(),
            product.Images
                .OrderByDescending(image => image.IsPrimary)
                .ThenBy(image => image.DisplayOrder)
                .Select(image => image.Url)
                .ToList(),
            product.Material,
            product.CareInstructions,
            product.SeoTitle,
            product.SeoDescription,
            product.RelatedProducts
                .Where(related =>
                    related.RelatedProduct.IsActive &&
                    !related.RelatedProduct.IsArchived)
                .OrderBy(related => related.DisplayOrder)
                .Select(related => new ProductCard(
                    related.RelatedProduct.Id,
                    related.RelatedProduct.Name,
                    related.RelatedProduct.Slug,
                    related.RelatedProduct.Price,
                    related.RelatedProduct.CompareAtPrice,
                    related.RelatedProduct.Images
                        .OrderByDescending(image => image.IsPrimary)
                        .ThenBy(image => image.DisplayOrder)
                        .Select(image => image.Url)
                        .FirstOrDefault() ?? related.RelatedProduct.ImageUrl,
                    related.RelatedProduct.Category.Name,
                    related.RelatedProduct.CreatedAtUtc >= DateTime.UtcNow.AddDays(-30)))
                .Take(4)
                .ToList());
}
