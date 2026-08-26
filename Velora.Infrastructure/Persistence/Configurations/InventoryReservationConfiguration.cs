using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Velora.Domain.Catalog;

namespace Velora.Infrastructure.Persistence.Configurations;

internal sealed class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.HasKey(reservation => reservation.Id);
        builder.HasIndex(reservation => new { reservation.ProductVariantId, reservation.ExpiresAtUtc, reservation.IsReleased });
        builder.HasOne(reservation => reservation.ProductVariant).WithMany(variant => variant.Reservations).HasForeignKey(reservation => reservation.ProductVariantId).OnDelete(DeleteBehavior.Cascade);
    }
}
