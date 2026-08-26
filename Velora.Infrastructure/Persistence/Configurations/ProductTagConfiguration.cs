using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Velora.Domain.Catalog;

namespace Velora.Infrastructure.Persistence.Configurations;

internal sealed class ProductTagConfiguration : IEntityTypeConfiguration<ProductTag>
{
    public void Configure(EntityTypeBuilder<ProductTag> builder)
    {
        builder.HasKey(productTag => new { productTag.ProductId, productTag.TagId });
        builder.HasOne(productTag => productTag.Product).WithMany(product => product.ProductTags).HasForeignKey(productTag => productTag.ProductId);
        builder.HasOne(productTag => productTag.Tag).WithMany(tag => tag.ProductTags).HasForeignKey(productTag => productTag.TagId);
    }
}
