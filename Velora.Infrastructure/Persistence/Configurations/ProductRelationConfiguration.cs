using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Velora.Domain.Catalog;

namespace Velora.Infrastructure.Persistence.Configurations;

internal sealed class ProductRelationConfiguration : IEntityTypeConfiguration<ProductRelation>
{
    public void Configure(EntityTypeBuilder<ProductRelation> builder)
    {
        builder.HasKey(relation => new { relation.ProductId, relation.RelatedProductId });
        builder.HasOne(relation => relation.Product).WithMany(product => product.RelatedProducts).HasForeignKey(relation => relation.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(relation => relation.RelatedProduct).WithMany().HasForeignKey(relation => relation.RelatedProductId).OnDelete(DeleteBehavior.NoAction);
    }
}
