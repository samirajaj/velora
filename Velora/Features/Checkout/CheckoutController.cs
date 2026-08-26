using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Velora.Application.Commerce;
using Velora.Application.Customers;
using Velora.Features.Cart;
using Microsoft.AspNetCore.RateLimiting;

namespace Velora.Features.Checkout;

[Authorize]
[Route("checkout")]
public sealed class CheckoutController(ICartService cart, ICheckoutService checkout, ICustomerAccountService accounts) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? couponCode, CancellationToken cancellationToken)
    {
        var cartModel = cart.GetCart(); if (cartModel.Items.Count == 0) return RedirectToAction("Index", "Cart");
        var profile = await accounts.GetProfileAsync(CustomerId(), cancellationToken); var address = (await accounts.GetAddressesAsync(CustomerId(), cancellationToken)).FirstOrDefault();
        return View(new CheckoutFormViewModel { CustomerEmail = profile?.Email ?? string.Empty, RecipientName = address?.RecipientName ?? $"{profile?.FirstName} {profile?.LastName}".Trim(), PhoneNumber = address?.PhoneNumber ?? profile?.PhoneNumber ?? string.Empty, AddressLine1 = address?.Line1 ?? string.Empty, AddressLine2 = address?.Line2 ?? string.Empty, City = address?.City ?? string.Empty, StateOrProvince = address?.StateOrProvince ?? string.Empty, PostalCode = address?.PostalCode ?? string.Empty, CountryCode = address?.CountryCode ?? "SY", CouponCode = couponCode, Cart = cartModel, Quote = await checkout.QuoteAsync(Lines(cartModel), couponCode, cancellationToken) });
    }
    [HttpPost(""), ValidateAntiForgeryToken, EnableRateLimiting("checkout")]
    public async Task<IActionResult> Place(CheckoutFormViewModel model, CancellationToken cancellationToken)
    {
        var cartModel = cart.GetCart(); if (cartModel.Items.Count == 0) return RedirectToAction("Index", "Cart"); Validate(model);
        if (!ModelState.IsValid) { model.Cart = cartModel; model.Quote = await checkout.QuoteAsync(Lines(cartModel), model.CouponCode, cancellationToken); return View("Index", model); }
        try { var result = await checkout.PlaceCashOnDeliveryOrderAsync(new CheckoutRequest { CustomerId = CustomerId(), CustomerEmail = model.CustomerEmail, RecipientName = model.RecipientName, PhoneNumber = model.PhoneNumber, AddressLine1 = model.AddressLine1, AddressLine2 = model.AddressLine2, City = model.City, StateOrProvince = model.StateOrProvince, PostalCode = model.PostalCode, CountryCode = model.CountryCode, CustomerNote = model.CustomerNote, CouponCode = model.CouponCode, Items = Lines(cartModel) }, cancellationToken); cart.Clear(); return RedirectToAction(nameof(Confirmation), new { id = result.Id }); }
        catch (InvalidOperationException exception) { ModelState.AddModelError(string.Empty, exception.Message); model.Cart = cartModel; model.Quote = await checkout.QuoteAsync(Lines(cartModel), model.CouponCode, cancellationToken); return View("Index", model); }
    }
    [HttpGet("confirmation/{id:guid}")] public async Task<IActionResult> Confirmation(Guid id, CancellationToken cancellationToken) { var order = await checkout.GetOrderAsync(id, CustomerId(), cancellationToken); return order is null ? NotFound() : View(order); }
    private static IReadOnlyList<CheckoutLine> Lines(CartViewModel model) => model.Items.Select(x => new CheckoutLine(x.ProductId, x.VariantId, x.Quantity)).ToList();
    private Guid CustomerId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private void Validate(CheckoutFormViewModel model) { if (string.IsNullOrWhiteSpace(model.CustomerEmail) || string.IsNullOrWhiteSpace(model.RecipientName) || string.IsNullOrWhiteSpace(model.PhoneNumber) || string.IsNullOrWhiteSpace(model.AddressLine1) || string.IsNullOrWhiteSpace(model.City) || model.CountryCode?.Length != 2) ModelState.AddModelError(string.Empty, "Email, recipient, phone, address, city, and a two-letter country code are required."); }
}
public sealed class CheckoutFormViewModel
{
    public string CustomerEmail { get; set; } = string.Empty; public string RecipientName { get; set; } = string.Empty; public string PhoneNumber { get; set; } = string.Empty; public string AddressLine1 { get; set; } = string.Empty; public string AddressLine2 { get; set; } = string.Empty; public string City { get; set; } = string.Empty; public string StateOrProvince { get; set; } = string.Empty; public string PostalCode { get; set; } = string.Empty; public string CountryCode { get; set; } = "SY"; public string CustomerNote { get; set; } = string.Empty; public string? CouponCode { get; set; } public CartViewModel Cart { get; set; } = new([]); public CheckoutQuote Quote { get; set; } = new(0, 0, 0, 0, "USD");
}
