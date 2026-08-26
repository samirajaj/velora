using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Velora.Domain.Customers;

namespace Velora.Infrastructure.Persistence.Configurations;

internal sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Label).HasMaxLength(50);
        builder.Property(x => x.RecipientName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Line1).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Line2).HasMaxLength(200);
        builder.Property(x => x.City).HasMaxLength(100).IsRequired();
        builder.Property(x => x.StateOrProvince).HasMaxLength(100);
        builder.Property(x => x.PostalCode).HasMaxLength(30);
        builder.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
        builder.HasIndex(x => new { x.CustomerId, x.IsArchived });
    }
}

internal sealed class CustomerCartConfiguration : IEntityTypeConfiguration<CustomerCart>
{
    public void Configure(EntityTypeBuilder<CustomerCart> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.CustomerId).IsUnique();
    }
}

internal sealed class CustomerCartItemConfiguration : IEntityTypeConfiguration<CustomerCartItem>
{
    public void Configure(EntityTypeBuilder<CustomerCartItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CustomerCartId, x.ProductVariantId }).IsUnique();
        builder.HasOne(x => x.CustomerCart).WithMany(x => x.Items).HasForeignKey(x => x.CustomerCartId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CustomerId, x.ProductId }).IsUnique();
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}
