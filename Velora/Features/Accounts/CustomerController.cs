using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Velora.Application.Commerce;
using Velora.Application.Customers;
using Velora.Infrastructure.Identity;
using System.Text.Encodings.Web;

namespace Velora.Features.Accounts;

[Authorize]
[Route("my-account")]
public sealed class CustomerController(ICustomerAccountService accounts, ICheckoutService checkout, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : Controller
{
    [HttpGet("")] public async Task<IActionResult> Index(CancellationToken cancellationToken) { var id = CustomerId(); return View(new CustomerDashboardViewModel(await accounts.GetProfileAsync(id, cancellationToken), await accounts.GetAddressesAsync(id, cancellationToken), await accounts.GetOrdersAsync(id, cancellationToken), await accounts.GetWishlistAsync(id, cancellationToken))); }
    [HttpPost("profile"), ValidateAntiForgeryToken] public async Task<IActionResult> Profile(CustomerProfile model, CancellationToken cancellationToken) { if (string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName)) TempData["AccountError"] = "First and last name are required."; else { await accounts.UpdateProfileAsync(CustomerId(), model, cancellationToken); TempData["AccountMessage"] = "Profile updated."; } return RedirectToAction(nameof(Index)); }
    [HttpGet("addresses/new")] public IActionResult NewAddress() => View("Address", new CustomerAddress());
    [HttpGet("addresses/{id:guid}")] public async Task<IActionResult> Address(Guid id, CancellationToken cancellationToken) { var model = await accounts.GetAddressAsync(CustomerId(), id, cancellationToken); return model is null ? NotFound() : View(model); }
    [HttpPost("addresses"), ValidateAntiForgeryToken] public async Task<IActionResult> SaveAddress(CustomerAddress model, CancellationToken cancellationToken) { if (string.IsNullOrWhiteSpace(model.RecipientName) || string.IsNullOrWhiteSpace(model.PhoneNumber) || string.IsNullOrWhiteSpace(model.Line1) || string.IsNullOrWhiteSpace(model.City) || model.CountryCode.Length != 2) { ModelState.AddModelError(string.Empty, "Recipient, phone, address, city, and two-letter country code are required."); return View("Address", model); } await accounts.SaveAddressAsync(CustomerId(), model, cancellationToken); return RedirectToAction(nameof(Index)); }
    [HttpPost("addresses/{id:guid}/archive"), ValidateAntiForgeryToken] public async Task<IActionResult> ArchiveAddress(Guid id, CancellationToken cancellationToken) { await accounts.ArchiveAddressAsync(CustomerId(), id, cancellationToken); return RedirectToAction(nameof(Index)); }
    [HttpPost("wishlist/{productId:guid}"), ValidateAntiForgeryToken] public async Task<IActionResult> Wishlist(Guid productId, string? returnUrl, CancellationToken cancellationToken) { await accounts.ToggleWishlistAsync(CustomerId(), productId, cancellationToken); return LocalRedirect(returnUrl ?? Url.Action(nameof(Index))!); }
    [HttpGet("orders/{id:guid}")] public async Task<IActionResult> Order(Guid id, CancellationToken cancellationToken) { var model = await checkout.GetOrderAsync(id, CustomerId(), cancellationToken); return model is null ? NotFound() : View(model); }
    [HttpPost("change-password"), ValidateAntiForgeryToken] public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model) { if (!ModelState.IsValid) { TempData["AccountError"] = "Review your password information."; return RedirectToAction(nameof(Index)); } var user = await userManager.GetUserAsync(User); var result = await userManager.ChangePasswordAsync(user!, model.CurrentPassword, model.NewPassword); if (result.Succeeded) { await signInManager.RefreshSignInAsync(user!); TempData["AccountMessage"] = "Password changed."; } else TempData["AccountError"] = string.Join(" ", result.Errors.Select(x => x.Description)); return RedirectToAction(nameof(Index)); }
    [HttpGet("two-factor")] public async Task<IActionResult> TwoFactor()
    {
        var user = await userManager.GetUserAsync(User); if (user is null) return Challenge();
        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key)) { await userManager.ResetAuthenticatorKeyAsync(user); key = await userManager.GetAuthenticatorKeyAsync(user); }
        var email = await userManager.GetEmailAsync(user) ?? user.UserName ?? "Velora";
        return View(new AuthenticatorViewModel { SharedKey = FormatKey(key!), AuthenticatorUri = GenerateQrCodeUri(email, key!) });
    }
    [HttpPost("two-factor"), ValidateAntiForgeryToken] public async Task<IActionResult> TwoFactor(AuthenticatorViewModel model)
    {
        var user = await userManager.GetUserAsync(User); if (user is null) return Challenge();
        var key = await userManager.GetAuthenticatorKeyAsync(user) ?? string.Empty;
        model.SharedKey = FormatKey(key); model.AuthenticatorUri = GenerateQrCodeUri(await userManager.GetEmailAsync(user) ?? "Velora", key);
        if (!ModelState.IsValid) return View(model);
        var code = model.Code.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        if (!await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, code)) { ModelState.AddModelError(nameof(model.Code), "The verification code is invalid."); return View(model); }
        await userManager.SetTwoFactorEnabledAsync(user, true); await signInManager.RefreshSignInAsync(user);
        var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 8);
        TempData["RecoveryCodes"] = string.Join(" ", recoveryCodes ?? []);
        TempData["AccountMessage"] = "Two-factor authentication is enabled. Save the recovery codes now.";
        return RedirectToAction(nameof(Index));
    }
    private Guid CustomerId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static string FormatKey(string key) => string.Join(" ", Enumerable.Range(0, (key.Length + 3) / 4).Select(i => key.Substring(i * 4, Math.Min(4, key.Length - i * 4)))).ToLowerInvariant();
    private static string GenerateQrCodeUri(string email, string key) => $"otpauth://totp/{UrlEncoder.Default.Encode("Velora")}:{UrlEncoder.Default.Encode(email)}?secret={key}&issuer={UrlEncoder.Default.Encode("Velora")}&digits=6";
}

public sealed record CustomerDashboardViewModel(CustomerProfile? Profile, IReadOnlyList<CustomerAddress> Addresses, IReadOnlyList<CustomerOrderSummary> Orders, IReadOnlyList<WishlistProduct> Wishlist);
