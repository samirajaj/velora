using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Velora.Domain.Catalog;

namespace Velora.Infrastructure.Persistence.Configurations;

internal sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Url).HasMaxLength(1_000).IsRequired();
        builder.Property(x => x.PublicId).HasMaxLength(300);
        builder.Property(x => x.AltText).HasMaxLength(200);
        builder.HasIndex(x => new { x.ProductId, x.DisplayOrder });
        builder.HasOne(x => x.Product).WithMany(x => x.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ProductVariant).WithMany(x => x.Images).HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class CollectionConfiguration : IEntityTypeConfiguration<Collection>
{
    public void Configure(EntityTypeBuilder<Collection> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(140).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1_000);
        builder.Property(x => x.ImageUrl).HasMaxLength(1_000);
        builder.Property(x => x.ImagePublicId).HasMaxLength(300);
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}

internal sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}

internal sealed class ProductTagConfiguration : IEntityTypeConfiguration<ProductTag>
{
    public void Configure(EntityTypeBuilder<ProductTag> builder)
    {
        builder.HasKey(x => new { x.ProductId, x.TagId });
        builder.HasOne(x => x.Product).WithMany(x => x.ProductTags).HasForeignKey(x => x.ProductId);
        builder.HasOne(x => x.Tag).WithMany(x => x.ProductTags).HasForeignKey(x => x.TagId);
    }
}

internal sealed class ProductRelationConfiguration : IEntityTypeConfiguration<ProductRelation>
{
    public void Configure(EntityTypeBuilder<ProductRelation> builder)
    {
        builder.HasKey(x => new { x.ProductId, x.RelatedProductId });
        builder.HasOne(x => x.Product).WithMany(x => x.RelatedProducts).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.RelatedProduct).WithMany().HasForeignKey(x => x.RelatedProductId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ProductVariantId, x.ExpiresAtUtc, x.IsReleased });
        builder.HasOne(x => x.ProductVariant).WithMany(x => x.Reservations).HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Cascade);
    }
}
