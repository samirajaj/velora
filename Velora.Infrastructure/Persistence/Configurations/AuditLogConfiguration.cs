using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Velora.Domain.Administration;

namespace Velora.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Action).HasMaxLength(100).IsRequired();
        builder.Property(log => log.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(log => log.EntityId).HasMaxLength(100);
        builder.Property(log => log.DetailsJson).HasMaxLength(8_000);
        builder.Property(log => log.IpAddress).HasMaxLength(64);
        builder.HasIndex(log => new { log.EntityName, log.EntityId, log.CreatedAtUtc });
    }
}
