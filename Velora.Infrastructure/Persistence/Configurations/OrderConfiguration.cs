using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Velora.Domain.Commerce;

namespace Velora.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Number).HasMaxLength(30).IsRequired();
        builder.HasIndex(order => order.Number).IsUnique();
        builder.HasIndex(order => new { order.CustomerId, order.CreatedAtUtc });
        builder.HasIndex(order => new { order.Status, order.CreatedAtUtc });
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(order => order.Subtotal).HasPrecision(18, 2);
        builder.Property(order => order.DiscountTotal).HasPrecision(18, 2);
        builder.Property(order => order.DeliveryTotal).HasPrecision(18, 2);
        builder.Property(order => order.Total).HasPrecision(18, 2);
        builder.Property(order => order.Currency).HasMaxLength(3);
        builder.Property(order => order.CouponCode).HasMaxLength(50);
        builder.Property(order => order.CustomerEmail).HasMaxLength(256);
        builder.Property(order => order.RecipientName).HasMaxLength(160);
        builder.Property(order => order.PhoneNumber).HasMaxLength(30);
        builder.Property(order => order.AddressLine1).HasMaxLength(200);
        builder.Property(order => order.AddressLine2).HasMaxLength(200);
        builder.Property(order => order.City).HasMaxLength(100);
        builder.Property(order => order.StateOrProvince).HasMaxLength(100);
        builder.Property(order => order.PostalCode).HasMaxLength(30);
        builder.Property(order => order.CountryCode).HasMaxLength(2);
        builder.Property(order => order.CustomerNote).HasMaxLength(1_000);
    }
}
