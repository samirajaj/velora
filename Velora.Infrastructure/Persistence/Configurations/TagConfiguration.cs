using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Velora.Domain.Catalog;

namespace Velora.Infrastructure.Persistence.Configurations;

internal sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(tag => tag.Id);
        builder.Property(tag => tag.Name).HasMaxLength(80).IsRequired();
        builder.Property(tag => tag.Slug).HasMaxLength(100).IsRequired();
        builder.HasIndex(tag => tag.Slug).IsUnique();
    }
}
