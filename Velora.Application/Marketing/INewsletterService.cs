namespace Velora.Application.Marketing;

public interface INewsletterService
{
    Task<bool> SubscribeAsync(string email, CancellationToken cancellationToken = default);
}
