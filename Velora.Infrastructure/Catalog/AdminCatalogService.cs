using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Velora.Application.Administration;
using Velora.Application.Media;
using Velora.Domain.Administration;
using Velora.Domain.Catalog;
using Velora.Infrastructure.Persistence;

namespace Velora.Infrastructure.Catalog;

internal sealed class AdminCatalogService(ApplicationDbContext context) : IAdminCatalogService
{
    public async Task<IReadOnlyList<AdminProductListItem>> GetProductsAsync(string? search, bool includeArchived, CancellationToken cancellationToken = default)
    {
        var query = context.Products.AsNoTracking().Where(x => includeArchived || !x.IsArchived);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Name.Contains(search) || x.Slug.Contains(search));
        return await query.OrderByDescending(x => x.CreatedAtUtc).Take(200).Select(x => new AdminProductListItem(x.Id, x.Name, x.Slug, x.Category.Name, x.Price, x.IsFeatured, x.IsActive, x.IsArchived, x.Variants.Sum(v => v.StockQuantity), x.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault() ?? x.ImageUrl)).ToListAsync(cancellationToken);
    }

    public async Task<AdminProductModel> CreateProductModelAsync(CancellationToken cancellationToken = default) =>
        await AddLookupsAsync(new AdminProductModel { Variants = [new()] }, cancellationToken);

    public async Task<AdminProductModel?> GetProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await context.Products.AsNoTracking().Where(x => x.Id == id).Select(x => new AdminProductModel
        {
            Id = x.Id, Name = x.Name, Slug = x.Slug, Description = x.Description, Price = x.Price, CompareAtPrice = x.CompareAtPrice,
            CategoryId = x.CategoryId, CollectionId = x.CollectionId, Material = x.Material, CareInstructions = x.CareInstructions,
            SeoTitle = x.SeoTitle, SeoDescription = x.SeoDescription, WeightGrams = x.WeightGrams, ShippingLengthCm = x.ShippingLengthCm,
            ShippingWidthCm = x.ShippingWidthCm, ShippingHeightCm = x.ShippingHeightCm, PublishAtUtc = x.PublishAtUtc,
            IsFeatured = x.IsFeatured, IsActive = x.IsActive,
            Variants = x.Variants.OrderBy(v => v.Sku).Select(v => new AdminVariantModel { Id = v.Id, Sku = v.Sku, Color = v.Color, ColorHex = v.ColorHex, Size = v.Size, StockQuantity = v.StockQuantity, LowStockThreshold = v.LowStockThreshold, IsActive = v.IsActive }).ToList(),
            Images = x.Images.OrderBy(i => i.DisplayOrder).Select(i => new AdminProductImage(i.Id, i.Url, i.PublicId, i.AltText, i.DisplayOrder, i.IsPrimary, i.ProductVariantId)).ToList()
        }).FirstOrDefaultAsync(cancellationToken);
        return model is null ? null : await AddLookupsAsync(model, cancellationToken);
    }

    public async Task<Guid> SaveProductAsync(AdminProductModel model, Guid actorId, string ipAddress, CancellationToken cancellationToken = default)
    {
        var product = model.Id is null ? new Product { Id = Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow } : await context.Products.Include(x => x.Variants).FirstOrDefaultAsync(x => x.Id == model.Id, cancellationToken) ?? throw new InvalidOperationException("Product not found.");
        var action = model.Id is null ? "Product.Created" : "Product.Updated";
        if (model.Id is null) context.Products.Add(product);
        product.Name = model.Name.Trim();
        product.Slug = await UniqueProductSlugAsync(string.IsNullOrWhiteSpace(model.Slug) ? model.Name : model.Slug, product.Id, cancellationToken);
        product.Description = model.Description.Trim(); product.Price = model.Price; product.CompareAtPrice = model.CompareAtPrice;
        product.CategoryId = model.CategoryId; product.CollectionId = model.CollectionId; product.Material = model.Material.Trim(); product.CareInstructions = model.CareInstructions.Trim();
        product.SeoTitle = model.SeoTitle.Trim(); product.SeoDescription = model.SeoDescription.Trim(); product.WeightGrams = model.WeightGrams;
        product.ShippingLengthCm = model.ShippingLengthCm; product.ShippingWidthCm = model.ShippingWidthCm; product.ShippingHeightCm = model.ShippingHeightCm;
        product.PublishAtUtc = model.PublishAtUtc; product.IsFeatured = model.IsFeatured; product.IsActive = model.IsActive; product.UpdatedAtUtc = DateTime.UtcNow;

        var submittedIds = model.Variants.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();
        foreach (var removed in product.Variants.Where(x => !submittedIds.Contains(x.Id))) removed.IsActive = false;
        foreach (var input in model.Variants.Where(x => !string.IsNullOrWhiteSpace(x.Sku)))
        {
            var variant = input.Id is null ? new ProductVariant { Id = Guid.NewGuid(), ProductId = product.Id } : product.Variants.FirstOrDefault(x => x.Id == input.Id) ?? throw new InvalidOperationException("Variant not found.");
            if (input.Id is null) product.Variants.Add(variant);
            variant.Sku = input.Sku.Trim().ToUpperInvariant(); variant.Color = input.Color.Trim(); variant.ColorHex = input.ColorHex.Trim(); variant.Size = input.Size.Trim().ToUpperInvariant();
            variant.StockQuantity = input.StockQuantity; variant.LowStockThreshold = input.LowStockThreshold; variant.IsActive = input.IsActive;
        }
        Audit(action, nameof(Product), product.Id, actorId, ipAddress, new { product.Name, product.Slug });
        await context.SaveChangesAsync(cancellationToken);
        return product.Id;
    }

    public async Task SetProductArchivedAsync(Guid id, bool archived, Guid actorId, string ipAddress, CancellationToken cancellationToken = default)
    {
        var product = await context.Products.FindAsync([id], cancellationToken) ?? throw new InvalidOperationException("Product not found.");
        product.IsArchived = archived; product.ArchivedAtUtc = archived ? DateTime.UtcNow : null; product.IsActive = !archived;
        Audit(archived ? "Product.Archived" : "Product.Restored", nameof(Product), id, actorId, ipAddress, null);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task SetProductFeaturedAsync(Guid id, bool featured, Guid actorId, string ipAddress, CancellationToken cancellationToken = default)
    {
        var product = await context.Products.FindAsync([id], cancellationToken) ?? throw new InvalidOperationException("Product not found.");
        product.IsFeatured = featured; Audit("Product.FeaturedChanged", nameof(Product), id, actorId, ipAddress, new { featured }); await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddImageAsync(Guid productId, MediaUploadResult upload, string altText, Guid? variantId, Guid actorId, string ipAddress, CancellationToken cancellationToken = default)
    {
        if (!await context.Products.AnyAsync(x => x.Id == productId, cancellationToken)) throw new InvalidOperationException("Product not found.");
        if (variantId.HasValue && !await context.ProductVariants.AnyAsync(x => x.Id == variantId && x.ProductId == productId, cancellationToken)) throw new InvalidOperationException("The selected variant does not belong to this product.");
        var order = await context.ProductImages.Where(x => x.ProductId == productId).Select(x => (int?)x.DisplayOrder).MaxAsync(cancellationToken) ?? -1;
        var hasPrimary = await context.ProductImages.AnyAsync(x => x.ProductId == productId && x.IsPrimary, cancellationToken);
        var image = new ProductImage { Id = Guid.NewGuid(), ProductId = productId, ProductVariantId = variantId, Url = upload.Url, PublicId = upload.PublicId, AltText = altText.Trim(), DisplayOrder = order + 1, IsPrimary = !hasPrimary };
        context.ProductImages.Add(image); Audit("Product.ImageAdded", nameof(ProductImage), image.Id, actorId, ipAddress, new { productId }); await context.SaveChangesAsync(cancellationToken);
    }

    public Task<AdminProductImage?> GetImageAsync(Guid imageId, CancellationToken cancellationToken = default) => context.ProductImages.AsNoTracking().Where(x => x.Id == imageId).Select(x => new AdminProductImage(x.Id, x.Url, x.PublicId, x.AltText, x.DisplayOrder, x.IsPrimary, x.ProductVariantId)).FirstOrDefaultAsync(cancellationToken);

    public async Task DeleteImageRecordAsync(Guid imageId, Guid actorId, string ipAddress, CancellationToken cancellationToken = default)
    {
        var image = await context.ProductImages.FindAsync([imageId], cancellationToken) ?? throw new InvalidOperationException("Image not found.");
        var productId = image.ProductId; var wasPrimary = image.IsPrimary; context.ProductImages.Remove(image);
        if (wasPrimary) { var next = await context.ProductImages.Where(x => x.ProductId == productId && x.Id != imageId).OrderBy(x => x.DisplayOrder).FirstOrDefaultAsync(cancellationToken); if (next is not null) next.IsPrimary = true; }
        Audit("Product.ImageDeleted", nameof(ProductImage), imageId, actorId, ipAddress, new { productId }); await context.SaveChangesAsync(cancellationToken);
    }

    public async Task SetPrimaryImageAsync(Guid productId, Guid imageId, Guid actorId, string ipAddress, CancellationToken cancellationToken = default)
    {
        if (!await context.ProductImages.AnyAsync(x => x.ProductId == productId && x.Id == imageId, cancellationToken)) throw new InvalidOperationException("Image not found for this product.");
        await context.ProductImages.Where(x => x.ProductId == productId).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsPrimary, x => x.Id == imageId), cancellationToken);
        Audit("Product.PrimaryImageChanged", nameof(Product), productId, actorId, ipAddress, new { imageId }); await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ReorderImagesAsync(Guid productId, IReadOnlyList<Guid> imageIds, Guid actorId, string ipAddress, CancellationToken cancellationToken = default)
    {
        var images = await context.ProductImages.Where(x => x.ProductId == productId).ToListAsync(cancellationToken);
        for (var index = 0; index < imageIds.Count; index++) { var image = images.FirstOrDefault(x => x.Id == imageIds[index]); if (image is not null) image.DisplayOrder = index; }
        Audit("Product.ImagesReordered", nameof(Product), productId, actorId, ipAddress, null); await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminCategoryListItem>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        await context.Categories.AsNoTracking().OrderBy(x => x.DisplayOrder).Select(x => new AdminCategoryListItem(x.Id, x.Name, x.Slug, x.Products.Count, x.IsActive, x.IsArchived)).ToListAsync(cancellationToken);

    public Task<AdminCategoryModel?> GetCategoryAsync(Guid id, CancellationToken cancellationToken = default) => context.Categories.AsNoTracking().Where(x => x.Id == id).Select(x => new AdminCategoryModel { Id = x.Id, Name = x.Name, Slug = x.Slug, Description = x.Description, DisplayOrder = x.DisplayOrder, IsActive = x.IsActive, SeoTitle = x.SeoTitle, SeoDescription = x.SeoDescription }).FirstOrDefaultAsync(cancellationToken);

    public async Task<Guid> SaveCategoryAsync(AdminCategoryModel model, Guid actorId, string ipAddress, CancellationToken cancellationToken = default)
    {
        var category = model.Id is null ? new Category { Id = Guid.NewGuid() } : await context.Categories.FindAsync([model.Id.Value], cancellationToken) ?? throw new InvalidOperationException("Category not found.");
        if (model.Id is null) context.Categories.Add(category);
        category.Name = model.Name.Trim(); category.Slug = await UniqueCategorySlugAsync(string.IsNullOrWhiteSpace(model.Slug) ? model.Name : model.Slug, category.Id, cancellationToken); category.Description = model.Description.Trim(); category.DisplayOrder = model.DisplayOrder; category.IsActive = model.IsActive; category.SeoTitle = model.SeoTitle.Trim(); category.SeoDescription = model.SeoDescription.Trim();
        Audit(model.Id is null ? "Category.Created" : "Category.Updated", nameof(Category), category.Id, actorId, ipAddress, new { category.Name, category.Slug }); await context.SaveChangesAsync(cancellationToken); return category.Id;
    }

    public async Task SetCategoryArchivedAsync(Guid id, bool archived, Guid actorId, string ipAddress, CancellationToken cancellationToken = default)
    {
        var category = await context.Categories.FindAsync([id], cancellationToken) ?? throw new InvalidOperationException("Category not found.");
        category.IsArchived = archived; category.ArchivedAtUtc = archived ? DateTime.UtcNow : null; category.IsActive = !archived;
        Audit(archived ? "Category.Archived" : "Category.Restored", nameof(Category), id, actorId, ipAddress, null); await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminCollectionListItem>> GetCollectionsAsync(CancellationToken cancellationToken = default) =>
        await context.Collections.AsNoTracking().OrderBy(x => x.Name).Select(x => new AdminCollectionListItem(x.Id, x.Name, x.Slug, x.Products.Count, x.IsFeatured, x.IsArchived, x.PublishAtUtc)).ToListAsync(cancellationToken);

    public Task<AdminCollectionModel?> GetCollectionAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Collections.AsNoTracking().Where(x => x.Id == id).Select(x => new AdminCollectionModel { Id = x.Id, Name = x.Name, Slug = x.Slug, Description = x.Description, IsFeatured = x.IsFeatured, IsArchived = x.IsArchived, PublishAtUtc = x.PublishAtUtc }).FirstOrDefaultAsync(cancellationToken);

    public async Task<Guid> SaveCollectionAsync(AdminCollectionModel model, Guid actorId, string ipAddress, CancellationToken cancellationToken = default)
    {
        var collection = model.Id is null ? new Collection { Id = Guid.NewGuid() } : await context.Collections.FindAsync([model.Id.Value], cancellationToken) ?? throw new InvalidOperationException("Collection not found.");
        if (model.Id is null) context.Collections.Add(collection);
        collection.Name = model.Name.Trim(); collection.Slug = await UniqueCollectionSlugAsync(string.IsNullOrWhiteSpace(model.Slug) ? model.Name : model.Slug, collection.Id, cancellationToken); collection.Description = model.Description.Trim(); collection.IsFeatured = model.IsFeatured; collection.PublishAtUtc = model.PublishAtUtc;
        Audit(model.Id is null ? "Collection.Created" : "Collection.Updated", nameof(Collection), collection.Id, actorId, ipAddress, new { collection.Name, collection.Slug }); await context.SaveChangesAsync(cancellationToken); return collection.Id;
    }

    public async Task SetCollectionArchivedAsync(Guid id, bool archived, Guid actorId, string ipAddress, CancellationToken cancellationToken = default)
    {
        var collection = await context.Collections.FindAsync([id], cancellationToken) ?? throw new InvalidOperationException("Collection not found."); collection.IsArchived = archived;
        Audit(archived ? "Collection.Archived" : "Collection.Restored", nameof(Collection), id, actorId, ipAddress, null); await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<AdminProductModel> AddLookupsAsync(AdminProductModel model, CancellationToken cancellationToken)
    {
        model.Categories = await context.Categories.AsNoTracking().Where(x => !x.IsArchived).OrderBy(x => x.DisplayOrder).Select(x => new AdminLookup(x.Id, x.Name)).ToListAsync(cancellationToken);
        model.Collections = await context.Collections.AsNoTracking().Where(x => !x.IsArchived).OrderBy(x => x.Name).Select(x => new AdminLookup(x.Id, x.Name)).ToListAsync(cancellationToken); return model;
    }
    private async Task<string> UniqueProductSlugAsync(string value, Guid id, CancellationToken cancellationToken) { var root = SlugGenerator.Generate(value); if (string.IsNullOrWhiteSpace(root)) root = "product"; var slug = root; var suffix = 2; while (await context.Products.AnyAsync(x => x.Id != id && x.Slug == slug, cancellationToken)) slug = $"{root}-{suffix++}"; return slug; }
    private async Task<string> UniqueCategorySlugAsync(string value, Guid id, CancellationToken cancellationToken) { var root = SlugGenerator.Generate(value); if (string.IsNullOrWhiteSpace(root)) root = "category"; var slug = root; var suffix = 2; while (await context.Categories.AnyAsync(x => x.Id != id && x.Slug == slug, cancellationToken)) slug = $"{root}-{suffix++}"; return slug; }
    private async Task<string> UniqueCollectionSlugAsync(string value, Guid id, CancellationToken cancellationToken) { var root = SlugGenerator.Generate(value); if (string.IsNullOrWhiteSpace(root)) root = "collection"; var slug = root; var suffix = 2; while (await context.Collections.AnyAsync(x => x.Id != id && x.Slug == slug, cancellationToken)) slug = $"{root}-{suffix++}"; return slug; }
    private void Audit(string action, string entity, Guid id, Guid actorId, string ipAddress, object? details) => context.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), UserId = actorId, Action = action, EntityName = entity, EntityId = id.ToString(), IpAddress = ipAddress, DetailsJson = details is null ? string.Empty : JsonSerializer.Serialize(details) });
}
