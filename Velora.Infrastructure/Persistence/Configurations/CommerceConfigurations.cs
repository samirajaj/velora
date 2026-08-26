using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Velora.Domain.Administration;
using Velora.Domain.Commerce;

namespace Velora.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Number).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.Number).IsUnique();
        builder.HasIndex(x => new { x.CustomerId, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.Status, x.CreatedAtUtc });
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Subtotal).HasPrecision(18, 2);
        builder.Property(x => x.DiscountTotal).HasPrecision(18, 2);
        builder.Property(x => x.DeliveryTotal).HasPrecision(18, 2);
        builder.Property(x => x.Total).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(3);
        builder.Property(x => x.CouponCode).HasMaxLength(50);
        builder.Property(x => x.CustomerEmail).HasMaxLength(256);
        builder.Property(x => x.RecipientName).HasMaxLength(160);
        builder.Property(x => x.PhoneNumber).HasMaxLength(30);
        builder.Property(x => x.AddressLine1).HasMaxLength(200);
        builder.Property(x => x.AddressLine2).HasMaxLength(200);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.StateOrProvince).HasMaxLength(100);
        builder.Property(x => x.PostalCode).HasMaxLength(30);
        builder.Property(x => x.CountryCode).HasMaxLength(2);
        builder.Property(x => x.CustomerNote).HasMaxLength(1_000);
    }
}

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductName).HasMaxLength(200);
        builder.Property(x => x.ProductSlug).HasMaxLength(220);
        builder.Property(x => x.Sku).HasMaxLength(80);
        builder.Property(x => x.Option).HasMaxLength(120);
        builder.Property(x => x.ImageUrl).HasMaxLength(1_000);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.LineTotal).HasPrecision(18, 2);
        builder.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Method).HasMaxLength(40);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.ExternalReference).HasMaxLength(200);
        builder.HasOne(x => x.Order).WithMany(x => x.Payments).HasForeignKey(x => x.OrderId);
    }
}

internal sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Carrier).HasMaxLength(100);
        builder.Property(x => x.TrackingNumber).HasMaxLength(150);
        builder.HasOne(x => x.Order).WithMany(x => x.Shipments).HasForeignKey(x => x.OrderId);
    }
}

internal sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.HasOne(x => x.Order).WithMany(x => x.StatusHistory).HasForeignKey(x => x.OrderId);
    }
}

internal sealed class DiscountCouponConfiguration : IEntityTypeConfiguration<DiscountCoupon>
{
    public void Configure(EntityTypeBuilder<DiscountCoupon> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Value).HasPrecision(18, 2);
        builder.Property(x => x.MinimumOrderAmount).HasPrecision(18, 2);
    }
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(100);
        builder.Property(x => x.DetailsJson).HasMaxLength(8_000);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.HasIndex(x => new { x.EntityName, x.EntityId, x.CreatedAtUtc });
    }
}
