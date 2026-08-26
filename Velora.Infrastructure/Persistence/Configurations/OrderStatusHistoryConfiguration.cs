using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Velora.Domain.Commerce;

namespace Velora.Infrastructure.Persistence.Configurations;

internal sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.HasKey(history => history.Id);
        builder.Property(history => history.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(history => history.Note).HasMaxLength(500);
        builder.HasOne(history => history.Order).WithMany(order => order.StatusHistory).HasForeignKey(history => history.OrderId);
    }
}
