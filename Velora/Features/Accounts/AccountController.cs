using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Velora.Infrastructure.Identity;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.RateLimiting;
using Velora.Application.Communication;
using Velora.Application.Customers;
using Velora.Features.Cart;

namespace Velora.Features.Accounts;

[Route("account")]
public sealed class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITransactionalEmailSender emailSender,
    ICustomerAccountService customerAccounts,
    ICartService cart) : Controller
{
    [HttpGet("register")]
    public IActionResult Register(string? returnUrl = null) => View(new RegisterViewModel());

    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim()
        };
        var result = await userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, AppRoles.Customer);
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var link = Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, token = encoded }, Request.Scheme)!;
            await emailSender.SendAsync(user.Email!, "Confirm your Velora account", $"<h1>Welcome to Velora</h1><p><a href=\"{link}\">Confirm your email address</a> to activate your account.</p>");
            return RedirectToAction(nameof(CheckEmail));
        }

        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        return View(model);
    }

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var account = await userManager.FindByEmailAsync(model.Email.Trim());
        if (account is not null && !account.IsActive) { ModelState.AddModelError(string.Empty, "This account is inactive. Please contact client care."); return View(model); }
        var result = await signInManager.PasswordSignInAsync(model.Email.Trim(), model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.RequiresTwoFactor)
            return RedirectToAction(nameof(LoginWithTwoFactor), new { model.ReturnUrl, model.RememberMe });
        if (result.Succeeded)
        {
            var user = await userManager.FindByEmailAsync(model.Email.Trim());
            if (user is not null)
            {
                var anonymousItems = cart.GetCart().Items.Select(x => new CustomerCartLine(x.ProductId, x.VariantId, x.Slug, x.Name, x.ImageUrl, x.Option, x.UnitPrice, x.Quantity)).ToList();
                await customerAccounts.MergeCartAsync(user.Id, anonymousItems);
                cart.Clear();
                var merged = await customerAccounts.GetCartAsync(user.Id);
                foreach (var item in merged) cart.Add(new CartItem(item.ProductId, item.VariantId, item.Slug, item.Name, item.ImageUrl, item.Option, item.UnitPrice, item.Quantity));
            }
            return LocalRedirect(model.ReturnUrl ?? Url.Action("Index", "Home")!);
        }
        ModelState.AddModelError(string.Empty, result.IsLockedOut ? "Your account is temporarily locked. Please try again later." : result.IsNotAllowed ? "Please confirm your email before signing in." : "The email or password is incorrect.");
        return View(model);
    }

    [HttpGet("login-2fa")]
    public IActionResult LoginWithTwoFactor(string? returnUrl = null, bool rememberMe = false) =>
        View(new TwoFactorLoginViewModel { ReturnUrl = returnUrl, RememberMe = rememberMe });

    [HttpPost("login-2fa"), ValidateAntiForgeryToken, EnableRateLimiting("auth")]
    public async Task<IActionResult> LoginWithTwoFactor(TwoFactorLoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var code = model.Code.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        var result = await signInManager.TwoFactorAuthenticatorSignInAsync(code, model.RememberMe, model.RememberMachine);
        if (result.Succeeded) return LocalRedirect(model.ReturnUrl ?? Url.Action("Index", "Home")!);
        ModelState.AddModelError(string.Empty, result.IsLockedOut ? "Your account is temporarily locked." : "The authenticator code is invalid.");
        return View(model);
    }

    [HttpGet("login-recovery-code")]
    public IActionResult LoginWithRecoveryCode(string? returnUrl = null) => View(new RecoveryCodeLoginViewModel { ReturnUrl = returnUrl });

    [HttpPost("login-recovery-code"), ValidateAntiForgeryToken, EnableRateLimiting("auth")]
    public async Task<IActionResult> LoginWithRecoveryCode(RecoveryCodeLoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await signInManager.TwoFactorRecoveryCodeSignInAsync(model.RecoveryCode.Replace(" ", string.Empty, StringComparison.Ordinal));
        if (result.Succeeded) return LocalRedirect(model.ReturnUrl ?? Url.Action("Index", "Home")!);
        ModelState.AddModelError(string.Empty, "The recovery code is invalid or has already been used.");
        return View(model);
    }

    [Authorize, HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("access-denied")]
    public IActionResult AccessDenied() => View();

    [HttpGet("check-email")]
    public IActionResult CheckEmail() => View();

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(Guid userId, string token)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()); if (user is null) return NotFound();
        var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token)); var result = await userManager.ConfirmEmailAsync(user, decoded);
        return View(result.Succeeded);
    }

    [HttpGet("forgot-password")]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost("forgot-password"), ValidateAntiForgeryToken, EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model); var user = await userManager.FindByEmailAsync(model.Email.Trim());
        if (user is not null && await userManager.IsEmailConfirmedAsync(user)) { var token = await userManager.GeneratePasswordResetTokenAsync(user); var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token)); var link = Url.Action(nameof(ResetPassword), "Account", new { email = model.Email, token = encoded }, Request.Scheme)!; await emailSender.SendAsync(model.Email, "Reset your Velora password", $"<p><a href=\"{link}\">Reset your password</a>. This link is intended only for you.</p>"); }
        return RedirectToAction(nameof(CheckEmail));
    }

    [HttpGet("reset-password")]
    public IActionResult ResetPassword(string email, string token) => View(new ResetPasswordViewModel { Email = email, Token = token });

    [HttpPost("reset-password"), ValidateAntiForgeryToken, EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model); var user = await userManager.FindByEmailAsync(model.Email); if (user is null) return RedirectToAction(nameof(Login));
        var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token)); var result = await userManager.ResetPasswordAsync(user, token, model.Password);
        if (result.Succeeded) return RedirectToAction(nameof(Login)); foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description); return View(model);
    }
}
