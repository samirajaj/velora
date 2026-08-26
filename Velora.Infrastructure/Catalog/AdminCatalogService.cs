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
    private const int ProductListLimit = 200;

    public async Task<IReadOnlyList<AdminProductListItem>> GetProductsAsync(
        string? search,
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        var query = context.Products
            .AsNoTracking()
            .Where(product => includeArchived || !product.IsArchived);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim();
            query = query.Where(product =>
                product.Name.Contains(searchTerm) ||
                product.Slug.Contains(searchTerm));
        }

        return await query
            .OrderByDescending(product => product.CreatedAtUtc)
            .Take(ProductListLimit)
            .Select(product => new AdminProductListItem(
                product.Id,
                product.Name,
                product.Slug,
                product.Category.Name,
                product.Price,
                product.IsFeatured,
                product.IsActive,
                product.IsArchived,
                product.Variants.Sum(variant => variant.StockQuantity),
                product.Images
                    .OrderBy(image => image.DisplayOrder)
                    .Select(image => image.Url)
                    .FirstOrDefault() ?? product.ImageUrl))
            .ToListAsync(cancellationToken);
    }

    public Task<AdminProductModel> CreateProductModelAsync(
        CancellationToken cancellationToken = default) =>
        AddLookupsAsync(
            new AdminProductModel { Variants = [new AdminVariantModel()] },
            cancellationToken);

    public async Task<AdminProductModel?> GetProductAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var model = await context.Products
            .AsNoTracking()
            .Where(product => product.Id == id)
            .Select(product => new AdminProductModel
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                Description = product.Description,
                Price = product.Price,
                CompareAtPrice = product.CompareAtPrice,
                CategoryId = product.CategoryId,
                CollectionId = product.CollectionId,
                Material = product.Material,
                CareInstructions = product.CareInstructions,
                SeoTitle = product.SeoTitle,
                SeoDescription = product.SeoDescription,
                WeightGrams = product.WeightGrams,
                ShippingLengthCm = product.ShippingLengthCm,
                ShippingWidthCm = product.ShippingWidthCm,
                ShippingHeightCm = product.ShippingHeightCm,
                PublishAtUtc = product.PublishAtUtc,
                IsFeatured = product.IsFeatured,
                IsActive = product.IsActive,
                Variants = product.Variants
                    .OrderBy(variant => variant.Sku)
                    .Select(variant => new AdminVariantModel
                    {
                        Id = variant.Id,
                        Sku = variant.Sku,
                        Color = variant.Color,
                        ColorHex = variant.ColorHex,
                        Size = variant.Size,
                        StockQuantity = variant.StockQuantity,
                        LowStockThreshold = variant.LowStockThreshold,
                        IsActive = variant.IsActive
                    })
                    .ToList(),
                Images = product.Images
                    .OrderBy(image => image.DisplayOrder)
                    .Select(image => new AdminProductImage(
                        image.Id,
                        image.Url,
                        image.PublicId,
                        image.AltText,
                        image.DisplayOrder,
                        image.IsPrimary,
                        image.ProductVariantId))
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return model is null
            ? null
            : await AddLookupsAsync(model, cancellationToken);
    }

    public async Task<Guid> SaveProductAsync(
        AdminProductModel model,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var isNew = model.Id is null;
        var product = isNew
            ? new Product { Id = Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow }
            : await context.Products
                .Include(candidate => candidate.Variants)
                .FirstOrDefaultAsync(candidate => candidate.Id == model.Id, cancellationToken)
              ?? throw new InvalidOperationException("Product not found.");

        if (isNew)
        {
            context.Products.Add(product);
        }

        await MapProductAsync(product, model, cancellationToken);
        SynchronizeVariants(product, model.Variants);
        AddAudit(
            isNew ? "Product.Created" : "Product.Updated",
            nameof(Product),
            product.Id,
            actorId,
            ipAddress,
            new { product.Name, product.Slug });

        await context.SaveChangesAsync(cancellationToken);
        return product.Id;
    }

    public async Task SetProductArchivedAsync(
        Guid id,
        bool archived,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var product = await context.Products.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Product not found.");

        product.IsArchived = archived;
        product.ArchivedAtUtc = archived ? DateTime.UtcNow : null;
        product.IsActive = !archived;
        AddAudit(
            archived ? "Product.Archived" : "Product.Restored",
            nameof(Product),
            id,
            actorId,
            ipAddress,
            null);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task SetProductFeaturedAsync(
        Guid id,
        bool featured,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var product = await context.Products.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Product not found.");

        product.IsFeatured = featured;
        AddAudit(
            "Product.FeaturedChanged",
            nameof(Product),
            id,
            actorId,
            ipAddress,
            new { featured });
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddImageAsync(
        Guid productId,
        MediaUploadResult upload,
        string altText,
        Guid? variantId,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        await EnsureProductAndVariantAsync(productId, variantId, cancellationToken);

        var lastDisplayOrder = await context.ProductImages
            .Where(image => image.ProductId == productId)
            .Select(image => (int?)image.DisplayOrder)
            .MaxAsync(cancellationToken) ?? -1;
        var hasPrimaryImage = await context.ProductImages.AnyAsync(
            image => image.ProductId == productId && image.IsPrimary,
            cancellationToken);

        var image = new ProductImage
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductVariantId = variantId,
            Url = upload.Url,
            PublicId = upload.PublicId,
            AltText = altText.Trim(),
            DisplayOrder = lastDisplayOrder + 1,
            IsPrimary = !hasPrimaryImage
        };

        context.ProductImages.Add(image);
        AddAudit(
            "Product.ImageAdded",
            nameof(ProductImage),
            image.Id,
            actorId,
            ipAddress,
            new { productId });
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<AdminProductImage?> GetImageAsync(
        Guid imageId,
        CancellationToken cancellationToken = default) =>
        context.ProductImages
            .AsNoTracking()
            .Where(image => image.Id == imageId)
            .Select(image => new AdminProductImage(
                image.Id,
                image.Url,
                image.PublicId,
                image.AltText,
                image.DisplayOrder,
                image.IsPrimary,
                image.ProductVariantId))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task DeleteImageRecordAsync(
        Guid imageId,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var image = await context.ProductImages.FindAsync([imageId], cancellationToken)
            ?? throw new InvalidOperationException("Image not found.");
        var productId = image.ProductId;
        var wasPrimary = image.IsPrimary;

        context.ProductImages.Remove(image);
        if (wasPrimary)
        {
            var nextImage = await context.ProductImages
                .Where(candidate =>
                    candidate.ProductId == productId && candidate.Id != imageId)
                .OrderBy(candidate => candidate.DisplayOrder)
                .FirstOrDefaultAsync(cancellationToken);
            if (nextImage is not null)
            {
                nextImage.IsPrimary = true;
            }
        }

        AddAudit(
            "Product.ImageDeleted",
            nameof(ProductImage),
            imageId,
            actorId,
            ipAddress,
            new { productId });
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task SetPrimaryImageAsync(
        Guid productId,
        Guid imageId,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var imageExists = await context.ProductImages.AnyAsync(
            image => image.ProductId == productId && image.Id == imageId,
            cancellationToken);
        if (!imageExists)
        {
            throw new InvalidOperationException("Image not found for this product.");
        }

        await context.ProductImages
            .Where(image => image.ProductId == productId)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(
                    image => image.IsPrimary,
                    image => image.Id == imageId),
                cancellationToken);

        AddAudit(
            "Product.PrimaryImageChanged",
            nameof(Product),
            productId,
            actorId,
            ipAddress,
            new { imageId });
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ReorderImagesAsync(
        Guid productId,
        IReadOnlyList<Guid> imageIds,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var images = await context.ProductImages
            .Where(image => image.ProductId == productId)
            .ToListAsync(cancellationToken);
        var submittedIds = imageIds.Distinct().ToList();

        if (submittedIds.Count != images.Count ||
            submittedIds.Any(id => images.All(image => image.Id != id)))
        {
            throw new InvalidOperationException("The image order is incomplete or invalid.");
        }

        for (var index = 0; index < submittedIds.Count; index++)
        {
            images.Single(image => image.Id == submittedIds[index]).DisplayOrder = index;
        }

        AddAudit(
            "Product.ImagesReordered",
            nameof(Product),
            productId,
            actorId,
            ipAddress,
            null);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminCategoryListItem>> GetCategoriesAsync(
        CancellationToken cancellationToken = default) =>
        await context.Categories
            .AsNoTracking()
            .OrderBy(category => category.DisplayOrder)
            .Select(category => new AdminCategoryListItem(
                category.Id,
                category.Name,
                category.Slug,
                category.Products.Count,
                category.IsActive,
                category.IsArchived))
            .ToListAsync(cancellationToken);

    public Task<AdminCategoryModel?> GetCategoryAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        context.Categories
            .AsNoTracking()
            .Where(category => category.Id == id)
            .Select(category => new AdminCategoryModel
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                DisplayOrder = category.DisplayOrder,
                IsActive = category.IsActive,
                SeoTitle = category.SeoTitle,
                SeoDescription = category.SeoDescription
            })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Guid> SaveCategoryAsync(
        AdminCategoryModel model,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var isNew = model.Id is null;
        var category = isNew
            ? new Category { Id = Guid.NewGuid() }
            : await context.Categories.FindAsync([model.Id!.Value], cancellationToken)
              ?? throw new InvalidOperationException("Category not found.");

        if (isNew)
        {
            context.Categories.Add(category);
        }

        category.Name = model.Name.Trim();
        category.Slug = await CreateUniqueCategorySlugAsync(
            string.IsNullOrWhiteSpace(model.Slug) ? model.Name : model.Slug,
            category.Id,
            cancellationToken);
        category.Description = model.Description.Trim();
        category.DisplayOrder = model.DisplayOrder;
        category.IsActive = model.IsActive;
        category.SeoTitle = model.SeoTitle.Trim();
        category.SeoDescription = model.SeoDescription.Trim();

        AddAudit(
            isNew ? "Category.Created" : "Category.Updated",
            nameof(Category),
            category.Id,
            actorId,
            ipAddress,
            new { category.Name, category.Slug });
        await context.SaveChangesAsync(cancellationToken);
        return category.Id;
    }

    public async Task SetCategoryArchivedAsync(
        Guid id,
        bool archived,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var category = await context.Categories.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Category not found.");

        category.IsArchived = archived;
        category.ArchivedAtUtc = archived ? DateTime.UtcNow : null;
        category.IsActive = !archived;
        AddAudit(
            archived ? "Category.Archived" : "Category.Restored",
            nameof(Category),
            id,
            actorId,
            ipAddress,
            null);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminCollectionListItem>> GetCollectionsAsync(
        CancellationToken cancellationToken = default) =>
        await context.Collections
            .AsNoTracking()
            .OrderBy(collection => collection.Name)
            .Select(collection => new AdminCollectionListItem(
                collection.Id,
                collection.Name,
                collection.Slug,
                collection.Products.Count,
                collection.IsFeatured,
                collection.IsArchived,
                collection.PublishAtUtc))
            .ToListAsync(cancellationToken);

    public Task<AdminCollectionModel?> GetCollectionAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        context.Collections
            .AsNoTracking()
            .Where(collection => collection.Id == id)
            .Select(collection => new AdminCollectionModel
            {
                Id = collection.Id,
                Name = collection.Name,
                Slug = collection.Slug,
                Description = collection.Description,
                IsFeatured = collection.IsFeatured,
                IsArchived = collection.IsArchived,
                PublishAtUtc = collection.PublishAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Guid> SaveCollectionAsync(
        AdminCollectionModel model,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var isNew = model.Id is null;
        var collection = isNew
            ? new Collection { Id = Guid.NewGuid() }
            : await context.Collections.FindAsync([model.Id!.Value], cancellationToken)
              ?? throw new InvalidOperationException("Collection not found.");

        if (isNew)
        {
            context.Collections.Add(collection);
        }

        collection.Name = model.Name.Trim();
        collection.Slug = await CreateUniqueCollectionSlugAsync(
            string.IsNullOrWhiteSpace(model.Slug) ? model.Name : model.Slug,
            collection.Id,
            cancellationToken);
        collection.Description = model.Description.Trim();
        collection.IsFeatured = model.IsFeatured;
        collection.PublishAtUtc = model.PublishAtUtc;

        AddAudit(
            isNew ? "Collection.Created" : "Collection.Updated",
            nameof(Collection),
            collection.Id,
            actorId,
            ipAddress,
            new { collection.Name, collection.Slug });
        await context.SaveChangesAsync(cancellationToken);
        return collection.Id;
    }

    public async Task SetCollectionArchivedAsync(
        Guid id,
        bool archived,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var collection = await context.Collections.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Collection not found.");

        collection.IsArchived = archived;
        AddAudit(
            archived ? "Collection.Archived" : "Collection.Restored",
            nameof(Collection),
            id,
            actorId,
            ipAddress,
            null);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task MapProductAsync(
        Product product,
        AdminProductModel model,
        CancellationToken cancellationToken)
    {
        product.Name = model.Name.Trim();
        product.Slug = await CreateUniqueProductSlugAsync(
            string.IsNullOrWhiteSpace(model.Slug) ? model.Name : model.Slug,
            product.Id,
            cancellationToken);
        product.Description = model.Description.Trim();
        product.Price = model.Price;
        product.CompareAtPrice = model.CompareAtPrice;
        product.CategoryId = model.CategoryId;
        product.CollectionId = model.CollectionId;
        product.Material = model.Material.Trim();
        product.CareInstructions = model.CareInstructions.Trim();
        product.SeoTitle = model.SeoTitle.Trim();
        product.SeoDescription = model.SeoDescription.Trim();
        product.WeightGrams = model.WeightGrams;
        product.ShippingLengthCm = model.ShippingLengthCm;
        product.ShippingWidthCm = model.ShippingWidthCm;
        product.ShippingHeightCm = model.ShippingHeightCm;
        product.PublishAtUtc = model.PublishAtUtc;
        product.IsFeatured = model.IsFeatured;
        product.IsActive = model.IsActive;
        product.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void SynchronizeVariants(
        Product product,
        IReadOnlyCollection<AdminVariantModel> models)
    {
        var submittedIds = models
            .Where(model => model.Id.HasValue)
            .Select(model => model.Id!.Value)
            .ToHashSet();

        foreach (var removedVariant in product.Variants
                     .Where(variant => !submittedIds.Contains(variant.Id)))
        {
            removedVariant.IsActive = false;
        }

        foreach (var model in models.Where(model => !string.IsNullOrWhiteSpace(model.Sku)))
        {
            var variant = FindOrCreateVariant(product, model);
            variant.Sku = model.Sku.Trim().ToUpperInvariant();
            variant.Color = model.Color.Trim();
            variant.ColorHex = model.ColorHex.Trim();
            variant.Size = model.Size.Trim().ToUpperInvariant();
            variant.StockQuantity = model.StockQuantity;
            variant.LowStockThreshold = model.LowStockThreshold;
            variant.IsActive = model.IsActive;
        }
    }

    private static ProductVariant FindOrCreateVariant(
        Product product,
        AdminVariantModel model)
    {
        if (model.Id.HasValue)
        {
            return product.Variants.FirstOrDefault(variant => variant.Id == model.Id)
                ?? throw new InvalidOperationException("Variant not found.");
        }

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id
        };
        product.Variants.Add(variant);
        return variant;
    }

    private async Task EnsureProductAndVariantAsync(
        Guid productId,
        Guid? variantId,
        CancellationToken cancellationToken)
    {
        if (!await context.Products.AnyAsync(
                product => product.Id == productId,
                cancellationToken))
        {
            throw new InvalidOperationException("Product not found.");
        }

        if (variantId.HasValue &&
            !await context.ProductVariants.AnyAsync(
                variant =>
                    variant.Id == variantId.Value && variant.ProductId == productId,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "The selected variant does not belong to this product.");
        }
    }

    private async Task<AdminProductModel> AddLookupsAsync(
        AdminProductModel model,
        CancellationToken cancellationToken)
    {
        model.Categories = await context.Categories
            .AsNoTracking()
            .Where(category => !category.IsArchived)
            .OrderBy(category => category.DisplayOrder)
            .Select(category => new AdminLookup(category.Id, category.Name))
            .ToListAsync(cancellationToken);
        model.Collections = await context.Collections
            .AsNoTracking()
            .Where(collection => !collection.IsArchived)
            .OrderBy(collection => collection.Name)
            .Select(collection => new AdminLookup(collection.Id, collection.Name))
            .ToListAsync(cancellationToken);
        return model;
    }

    private async Task<string> CreateUniqueProductSlugAsync(
        string value,
        Guid id,
        CancellationToken cancellationToken)
    {
        var root = CreateSlugRoot(value, "product");
        var slug = root;
        var suffix = 2;
        while (await context.Products.AnyAsync(
                   product => product.Id != id && product.Slug == slug,
                   cancellationToken))
        {
            slug = $"{root}-{suffix++}";
        }

        return slug;
    }

    private async Task<string> CreateUniqueCategorySlugAsync(
        string value,
        Guid id,
        CancellationToken cancellationToken)
    {
        var root = CreateSlugRoot(value, "category");
        var slug = root;
        var suffix = 2;
        while (await context.Categories.AnyAsync(
                   category => category.Id != id && category.Slug == slug,
                   cancellationToken))
        {
            slug = $"{root}-{suffix++}";
        }

        return slug;
    }

    private async Task<string> CreateUniqueCollectionSlugAsync(
        string value,
        Guid id,
        CancellationToken cancellationToken)
    {
        var root = CreateSlugRoot(value, "collection");
        var slug = root;
        var suffix = 2;
        while (await context.Collections.AnyAsync(
                   collection => collection.Id != id && collection.Slug == slug,
                   cancellationToken))
        {
            slug = $"{root}-{suffix++}";
        }

        return slug;
    }

    private static string CreateSlugRoot(string value, string fallback)
    {
        var slug = SlugGenerator.Generate(value);
        return string.IsNullOrWhiteSpace(slug) ? fallback : slug;
    }

    private void AddAudit(
        string action,
        string entity,
        Guid id,
        Guid actorId,
        string ipAddress,
        object? details)
    {
        context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = action,
            EntityName = entity,
            EntityId = id.ToString(),
            IpAddress = ipAddress,
            DetailsJson = details is null
                ? string.Empty
                : JsonSerializer.Serialize(details)
        });
    }
}
