using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Velora.Application.Communication;
using Velora.Application.Marketing;
using Velora.Configuration;

namespace Velora.Features.Newsletter;

[Route("newsletter")]
public sealed class NewsletterController(
    ITransactionalEmailSender emailSender,
    INewsletterService newsletterService,
    IOptions<SiteOptions> siteOptions,
    ILogger<NewsletterController> logger) : Controller
{
    [HttpPost("subscribe")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("newsletter")]
    public async Task<IActionResult> Subscribe(
        NewsletterSubscriptionViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["NewsletterError"] = "Enter a valid email address.";
            return RedirectToFooter();
        }

        var subscriberEmail = model.Email.Trim().ToLowerInvariant();
        var encodedEmail = HtmlEncoder.Default.Encode(subscriberEmail);
        var contactEmail = siteOptions.Value.ContactEmail;
        var isNewSubscription = await newsletterService.SubscribeAsync(
            subscriberEmail,
            cancellationToken);

        if (!isNewSubscription)
        {
            TempData["NewsletterMessage"] = "You are already subscribed to Private Notes.";
            return RedirectToFooter();
        }

        try
        {
            await emailSender.SendAsync(
                contactEmail,
                "New Velora newsletter subscription",
                $"<p><strong>{encodedEmail}</strong> subscribed to Private Notes.</p>",
                cancellationToken);

            await emailSender.SendAsync(
                subscriberEmail,
                "Welcome to Velora Private Notes",
                "<p>Thank you for joining Velora Private Notes. We will share new collections and stories occasionally.</p>",
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Newsletter subscription was stored, but notification email delivery failed");
        }

        TempData["NewsletterMessage"] = "Thank you. You are now subscribed to Private Notes.";
        return RedirectToFooter();
    }

    private RedirectResult RedirectToFooter() =>
        Redirect($"{Url.Action("Index", "Home")}#private-notes");
}
