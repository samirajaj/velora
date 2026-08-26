using System.ComponentModel.DataAnnotations;

namespace Velora.Features.Newsletter;

public sealed class NewsletterSubscriptionViewModel
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;
}
