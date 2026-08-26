using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Velora.Domain.Commerce;

namespace Velora.Infrastructure.Persistence.Configurations;

internal sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.HasKey(shipment => shipment.Id);
        builder.Property(shipment => shipment.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(shipment => shipment.Carrier).HasMaxLength(100);
        builder.Property(shipment => shipment.TrackingNumber).HasMaxLength(150);
        builder.HasOne(shipment => shipment.Order).WithMany(order => order.Shipments).HasForeignKey(shipment => shipment.OrderId);
    }
}
