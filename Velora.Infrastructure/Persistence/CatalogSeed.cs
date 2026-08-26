using Microsoft.EntityFrameworkCore;
using Velora.Domain.Catalog;

namespace Velora.Infrastructure.Persistence;

public static class CatalogSeed
{
    private static readonly Guid DressesId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid TailoringId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid AccessoriesId = Guid.Parse("10000000-0000-0000-0000-000000000003");

    public static async Task InitializeAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        await context.Database.MigrateAsync(cancellationToken);

        if (!await context.Categories.AnyAsync(cancellationToken))
        {
            var categories = new[]
            {
            new Category { Id = DressesId, Name = "Dresses", Slug = "dresses", Description = "Fluid silhouettes made for memorable entrances.", ImageUrl = "https://images.unsplash.com/photo-1566174053879-31528523f8ae?auto=format&fit=crop&w=1000&q=85", DisplayOrder = 1 },
            new Category { Id = TailoringId, Name = "Tailoring", Slug = "tailoring", Description = "Modern structure with an effortless point of view.", ImageUrl = "https://images.unsplash.com/photo-1591369822096-ffd140ec948f?auto=format&fit=crop&w=1000&q=85", DisplayOrder = 2 },
            new Category { Id = AccessoriesId, Name = "Accessories", Slug = "accessories", Description = "The finishing details, considered beautifully.", ImageUrl = "https://images.unsplash.com/photo-1594223274512-ad4803739b7c?auto=format&fit=crop&w=1000&q=85", DisplayOrder = 3 }
            };

            var products = new[]
            {
            Product("20000000-0000-0000-0000-000000000001", DressesId, "The Celeste Gown", "the-celeste-gown", 289m, 349m, "A sculptural evening gown cut on the bias with a softly draped neckline.", "https://images.unsplash.com/photo-1566174053879-31528523f8ae?auto=format&fit=crop&w=1200&q=88", true, -2),
            Product("20000000-0000-0000-0000-000000000002", TailoringId, "Noir Column Blazer", "noir-column-blazer", 219m, null, "Longline tailoring with a precise shoulder and clean single-button closure.", "https://images.unsplash.com/photo-1591369822096-ffd140ec948f?auto=format&fit=crop&w=1200&q=88", true, -8),
            Product("20000000-0000-0000-0000-000000000003", AccessoriesId, "Aurelia Mini Bag", "aurelia-mini-bag", 149m, 179m, "A compact architectural bag finished with brushed gold hardware.", "https://images.unsplash.com/photo-1594223274512-ad4803739b7c?auto=format&fit=crop&w=1200&q=88", true, -15),
            Product("20000000-0000-0000-0000-000000000004", DressesId, "Sienna Silk Dress", "sienna-silk-dress", 239m, null, "Pure silk, a refined cowl neck, and an elegant ankle-skimming line.", "https://images.unsplash.com/photo-1572804013309-59a88b7e92f1?auto=format&fit=crop&w=1200&q=88", true, -25),
            Product("20000000-0000-0000-0000-000000000005", TailoringId, "Ivory Wide-Leg Trouser", "ivory-wide-leg-trouser", 129m, null, "High-waisted trousers with flowing volume and a sharp pressed crease.", "https://images.unsplash.com/photo-1594633312681-425c7b97ccd1?auto=format&fit=crop&w=1200&q=88", false, -40),
            Product("20000000-0000-0000-0000-000000000006", AccessoriesId, "Sculpted Gold Cuff", "sculpted-gold-cuff", 89m, null, "An organic statement cuff with a softly hammered, luminous finish.", "https://images.unsplash.com/photo-1611652022419-a9419f74343d?auto=format&fit=crop&w=1200&q=88", false, -55)
            };

            await context.Categories.AddRangeAsync(categories, cancellationToken);
            await context.Products.AddRangeAsync(products, cancellationToken);
            await context.ProductVariants.AddRangeAsync(CreateVariants(products), cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        var productsWithoutImages = await context.Products
            .Where(x => !x.Images.Any() && x.ImageUrl != string.Empty)
            .ToListAsync(cancellationToken);
        if (productsWithoutImages.Count > 0)
        {
            await context.ProductImages.AddRangeAsync(productsWithoutImages.Select(product => new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Url = product.ImageUrl,
                PublicId = product.ImagePublicId ?? string.Empty,
                AltText = product.Name,
                IsPrimary = true,
                DisplayOrder = 0
            }), cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static Product Product(string id, Guid categoryId, string name, string slug, decimal price, decimal? compareAt, string description, string image, bool featured, int daysOld) =>
        new() { Id = Guid.Parse(id), CategoryId = categoryId, Name = name, Slug = slug, Price = price, CompareAtPrice = compareAt, Description = description, ImageUrl = image, IsFeatured = featured, CreatedAtUtc = DateTime.UtcNow.AddDays(daysOld) };

    private static IEnumerable<ProductVariant> CreateVariants(IEnumerable<Product> products)
    {
        var sizes = new[] { "XS", "S", "M", "L" };
        var index = 1;
        foreach (var product in products)
        {
            foreach (var size in sizes)
            {
                yield return new ProductVariant
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Sku = $"VEL-{index:000}-{size}",
                    Color = index % 2 == 0 ? "Ivory" : "Black",
                    ColorHex = index % 2 == 0 ? "#F1EDE5" : "#171717",
                    Size = size,
                    StockQuantity = 8 + index
                };
            }
            index++;
        }
    }
}
