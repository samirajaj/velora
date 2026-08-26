using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Velora.Domain.Marketing;

namespace Velora.Infrastructure.Persistence.Configurations;

internal sealed class NewsletterSubscriberConfiguration : IEntityTypeConfiguration<NewsletterSubscriber>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscriber> builder)
    {
        builder.HasKey(subscriber => subscriber.Id);
        builder.Property(subscriber => subscriber.Email).HasMaxLength(256).IsRequired();
        builder.Property(subscriber => subscriber.NormalizedEmail).HasMaxLength(256).IsRequired();
        builder.HasIndex(subscriber => subscriber.NormalizedEmail).IsUnique();
        builder.HasIndex(subscriber => new { subscriber.IsActive, subscriber.CreatedAtUtc });
    }
}
