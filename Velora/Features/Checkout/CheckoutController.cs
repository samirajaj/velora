using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Velora.Application.Commerce;
using Velora.Application.Customers;
using Velora.Features.Cart;

namespace Velora.Features.Checkout;

[Authorize]
[Route("checkout")]
public sealed class CheckoutController(
    ICartService cart,
    ICheckoutService checkout,
    ICustomerAccountService accounts) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? couponCode,
        CancellationToken cancellationToken)
    {
        var cartModel = cart.GetCart();
        if (cartModel.Items.Count == 0)
        {
            return RedirectToAction("Index", "Cart");
        }

        var customerId = CustomerId;
        var profile = await accounts.GetProfileAsync(customerId, cancellationToken);
        var address = (await accounts.GetAddressesAsync(customerId, cancellationToken))
            .FirstOrDefault();

        var model = new CheckoutFormViewModel
        {
            CustomerEmail = profile?.Email ?? string.Empty,
            RecipientName = address?.RecipientName
                ?? $"{profile?.FirstName} {profile?.LastName}".Trim(),
            PhoneNumber = address?.PhoneNumber ?? profile?.PhoneNumber ?? string.Empty,
            AddressLine1 = address?.Line1 ?? string.Empty,
            AddressLine2 = address?.Line2 ?? string.Empty,
            City = address?.City ?? string.Empty,
            StateOrProvince = address?.StateOrProvince ?? string.Empty,
            PostalCode = address?.PostalCode ?? string.Empty,
            CountryCode = address?.CountryCode ?? "SY",
            CouponCode = couponCode,
            Cart = cartModel,
            Quote = await checkout.QuoteAsync(
                CreateCheckoutLines(cartModel),
                couponCode,
                cancellationToken)
        };

        return View(model);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("checkout")]
    public async Task<IActionResult> Place(
        CheckoutFormViewModel model,
        CancellationToken cancellationToken)
    {
        var cartModel = cart.GetCart();
        if (cartModel.Items.Count == 0)
        {
            return RedirectToAction("Index", "Cart");
        }

        if (!ModelState.IsValid)
        {
            return await ReturnCheckoutViewAsync(model, cartModel, cancellationToken);
        }

        try
        {
            var result = await checkout.PlaceCashOnDeliveryOrderAsync(
                CreateCheckoutRequest(model, cartModel),
                cancellationToken);

            cart.Clear();
            return RedirectToAction(nameof(Confirmation), new { id = result.Id });
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return await ReturnCheckoutViewAsync(model, cartModel, cancellationToken);
        }
    }

    [HttpGet("confirmation/{id:guid}")]
    public async Task<IActionResult> Confirmation(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await checkout.GetOrderAsync(id, CustomerId, cancellationToken);
        return order is null ? NotFound() : View(order);
    }

    private Guid CustomerId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<IActionResult> ReturnCheckoutViewAsync(
        CheckoutFormViewModel model,
        CartViewModel cartModel,
        CancellationToken cancellationToken)
    {
        model.Cart = cartModel;
        model.Quote = await checkout.QuoteAsync(
            CreateCheckoutLines(cartModel),
            model.CouponCode,
            cancellationToken);
        return View("Index", model);
    }

    private CheckoutRequest CreateCheckoutRequest(
        CheckoutFormViewModel model,
        CartViewModel cartModel) =>
        new()
        {
            CustomerId = CustomerId,
            CustomerEmail = model.CustomerEmail,
            RecipientName = model.RecipientName,
            PhoneNumber = model.PhoneNumber,
            AddressLine1 = model.AddressLine1,
            AddressLine2 = model.AddressLine2,
            City = model.City,
            StateOrProvince = model.StateOrProvince,
            PostalCode = model.PostalCode,
            CountryCode = model.CountryCode,
            CustomerNote = model.CustomerNote,
            CouponCode = model.CouponCode,
            Items = CreateCheckoutLines(cartModel)
        };

    private static IReadOnlyList<CheckoutLine> CreateCheckoutLines(CartViewModel model) =>
        model.Items
            .Select(item => new CheckoutLine(
                item.ProductId,
                item.VariantId,
                item.Quantity))
            .ToList();
}
