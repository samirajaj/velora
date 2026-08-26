using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Velora.Application.Communication;

namespace Velora.Infrastructure.Communication;

public sealed class EmailOptions
{
    public const string SectionName = "Email";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "hello@velora.com";
    public string FromName { get; set; } = "Velora";
    public bool EnableSsl { get; set; } = true;
}

internal sealed class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : ITransactionalEmailSender
{
    public async Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Host))
        {
            logger.LogWarning("Transactional email is not configured. Skipped email {Subject} to {Recipient}", subject, recipient);
            return;
        }
        using var message = new MailMessage(new MailAddress(settings.FromAddress, settings.FromName), new MailAddress(recipient)) { Subject = subject, Body = htmlBody, IsBodyHtml = true };
        using var client = new SmtpClient(settings.Host, settings.Port) { EnableSsl = settings.EnableSsl, Credentials = new NetworkCredential(settings.UserName, settings.Password) };
        await client.SendMailAsync(message, cancellationToken);
    }
}
