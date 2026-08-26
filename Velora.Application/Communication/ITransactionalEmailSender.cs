namespace Velora.Application.Communication;

public interface ITransactionalEmailSender
{
    Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
