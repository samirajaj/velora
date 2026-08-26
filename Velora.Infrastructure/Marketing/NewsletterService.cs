using Microsoft.EntityFrameworkCore;
using Velora.Application.Marketing;
using Velora.Domain.Marketing;
using Velora.Infrastructure.Persistence;

namespace Velora.Infrastructure.Marketing;

internal sealed class NewsletterService(ApplicationDbContext context) : INewsletterService
{
    public async Task<bool> SubscribeAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var existing = await context.NewsletterSubscribers
            .SingleOrDefaultAsync(
                subscriber => subscriber.NormalizedEmail == normalizedEmail,
                cancellationToken);

        if (existing is not null)
        {
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                await context.SaveChangesAsync(cancellationToken);
            }

            return false;
        }

        context.NewsletterSubscribers.Add(new NewsletterSubscriber
        {
            Id = Guid.NewGuid(),
            Email = email.Trim(),
            NormalizedEmail = normalizedEmail
        });

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
