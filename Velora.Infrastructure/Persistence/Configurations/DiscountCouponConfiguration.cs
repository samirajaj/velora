using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Velora.Domain.Commerce;

namespace Velora.Infrastructure.Persistence.Configurations;

internal sealed class DiscountCouponConfiguration : IEntityTypeConfiguration<DiscountCoupon>
{
    public void Configure(EntityTypeBuilder<DiscountCoupon> builder)
    {
        builder.HasKey(coupon => coupon.Id);
        builder.Property(coupon => coupon.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(coupon => coupon.Code).IsUnique();
        builder.Property(coupon => coupon.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(coupon => coupon.Value).HasPrecision(18, 2);
        builder.Property(coupon => coupon.MinimumOrderAmount).HasPrecision(18, 2);
    }
}
