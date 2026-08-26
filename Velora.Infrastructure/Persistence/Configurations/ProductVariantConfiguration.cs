using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Velora.Domain.Catalog;

namespace Velora.Infrastructure.Persistence.Configurations;

internal sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Sku).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Color).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ColorHex).HasMaxLength(7).IsRequired();
        builder.Property(x => x.Size).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.Sku).IsUnique();
        builder.HasOne(x => x.Product)
            .WithMany(x => x.Variants)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
