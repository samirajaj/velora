using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Velora.Domain.Catalog;

namespace Velora.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(220).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4_000);
        builder.Property(x => x.Price).HasPrecision(18, 2);
        builder.Property(x => x.CompareAtPrice).HasPrecision(18, 2);
        builder.Property(x => x.ImageUrl).HasMaxLength(1_000);
        builder.Property(x => x.ImagePublicId).HasMaxLength(300);
        builder.Property(x => x.Material).HasMaxLength(1_000);
        builder.Property(x => x.CareInstructions).HasMaxLength(2_000);
        builder.Property(x => x.SeoTitle).HasMaxLength(160);
        builder.Property(x => x.SeoDescription).HasMaxLength(320);
        builder.Property(x => x.ShippingLengthCm).HasPrecision(8, 2);
        builder.Property(x => x.ShippingWidthCm).HasPrecision(8, 2);
        builder.Property(x => x.ShippingHeightCm).HasPrecision(8, 2);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.IsArchived, x.IsFeatured, x.CreatedAtUtc });
        builder.HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Collection)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CollectionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
