using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Velora.Domain.Catalog;

namespace Velora.Infrastructure.Persistence.Configurations;

internal sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.HasKey(image => image.Id);
        builder.Property(image => image.Url).HasMaxLength(1_000).IsRequired();
        builder.Property(image => image.PublicId).HasMaxLength(300);
        builder.Property(image => image.AltText).HasMaxLength(200);
        builder.HasIndex(image => new { image.ProductId, image.DisplayOrder });
        builder.HasOne(image => image.Product).WithMany(product => product.Images).HasForeignKey(image => image.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(image => image.ProductVariant).WithMany(variant => variant.Images).HasForeignKey(image => image.ProductVariantId).OnDelete(DeleteBehavior.NoAction);
    }
}
