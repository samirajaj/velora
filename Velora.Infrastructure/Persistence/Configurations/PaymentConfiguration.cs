using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Velora.Domain.Commerce;

namespace Velora.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Method).HasMaxLength(40);
        builder.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(payment => payment.Amount).HasPrecision(18, 2);
        builder.Property(payment => payment.ExternalReference).HasMaxLength(200);
        builder.HasOne(payment => payment.Order).WithMany(order => order.Payments).HasForeignKey(payment => payment.OrderId);
    }
}
