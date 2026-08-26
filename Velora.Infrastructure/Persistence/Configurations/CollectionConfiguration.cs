using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Velora.Domain.Catalog;

namespace Velora.Infrastructure.Persistence.Configurations;

internal sealed class CollectionConfiguration : IEntityTypeConfiguration<Collection>
{
    public void Configure(EntityTypeBuilder<Collection> builder)
    {
        builder.HasKey(collection => collection.Id);
        builder.Property(collection => collection.Name).HasMaxLength(120).IsRequired();
        builder.Property(collection => collection.Slug).HasMaxLength(140).IsRequired();
        builder.Property(collection => collection.Description).HasMaxLength(1_000);
        builder.Property(collection => collection.ImageUrl).HasMaxLength(1_000);
        builder.Property(collection => collection.ImagePublicId).HasMaxLength(300);
        builder.HasIndex(collection => collection.Slug).IsUnique();
    }
}
