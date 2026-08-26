using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Velora.Domain.Customers;

namespace Velora.Infrastructure.Persistence.Configurations;

internal sealed class CustomerCartItemConfiguration : IEntityTypeConfiguration<CustomerCartItem>
{
    public void Configure(EntityTypeBuilder<CustomerCartItem> builder)
    {
        builder.HasKey(item => item.Id);
        builder.HasIndex(item => new { item.CustomerCartId, item.ProductVariantId }).IsUnique();
        builder.HasOne(item => item.CustomerCart).WithMany(cart => cart.Items).HasForeignKey(item => item.CustomerCartId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.ProductVariant).WithMany().HasForeignKey(item => item.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
    }
}
