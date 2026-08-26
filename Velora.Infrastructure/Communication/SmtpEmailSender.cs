using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Velora.Application.Communication;

namespace Velora.Infrastructure.Communication;

internal sealed class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : ITransactionalEmailSender
{
    public async Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            logger.LogWarning("Transactional email is not configured. Skipped email {Subject} to {Recipient}", subject, recipient);
            return;
        }

        using var message = new MailMessage(
            new MailAddress(settings.FromAddress, settings.FromName),
            new MailAddress(recipient))
        {
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.EnableSsl,
            Credentials = new NetworkCredential(settings.UserName, settings.Password)
        };

        await client.SendMailAsync(message, cancellationToken);
    }
}
