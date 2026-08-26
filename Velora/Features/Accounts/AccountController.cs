using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Velora.Application.Communication;
using Velora.Application.Customers;
using Velora.Configuration;
using Velora.Features.Cart;
using Velora.Infrastructure.Identity;

namespace Velora.Features.Accounts;

[Route("account")]
public sealed class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITransactionalEmailSender emailSender,
    ICustomerAccountService customerAccounts,
    ICartService cart,
    IOptions<SiteOptions> siteOptions) : Controller
{
    [HttpGet("register")]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = normalizedEmail,
            Email = normalizedEmail,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim()
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }

        await userManager.AddToRoleAsync(user, AppRoles.Customer);
        await SendConfirmationEmailAsync(user);
        return RedirectToAction(nameof(CheckEmail));
    }

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        var account = await userManager.FindByEmailAsync(email);
        if (account is not null && !account.IsActive)
        {
            ModelState.AddModelError(string.Empty, "This account is inactive. Please contact client care.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(
            email,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.RequiresTwoFactor)
        {
            return RedirectToAction(
                nameof(LoginWithTwoFactor),
                new { model.ReturnUrl, model.RememberMe });
        }

        if (result.Succeeded)
        {
            if (account is not null)
            {
                await MergeCartAsync(account.Id);
            }

            return RedirectToLocal(model.ReturnUrl);
        }

        var error = result.IsLockedOut
            ? "Your account is temporarily locked. Please try again later."
            : result.IsNotAllowed
                ? "Please confirm your email before signing in."
                : "The email or password is incorrect.";

        ModelState.AddModelError(string.Empty, error);
        return View(model);
    }

    [HttpGet("login-2fa")]
    public IActionResult LoginWithTwoFactor(string? returnUrl = null, bool rememberMe = false) =>
        View(new TwoFactorLoginViewModel
        {
            ReturnUrl = returnUrl,
            RememberMe = rememberMe
        });

    [HttpPost("login-2fa")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> LoginWithTwoFactor(TwoFactorLoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        var result = await signInManager.TwoFactorAuthenticatorSignInAsync(
            NormalizeSecurityCode(model.Code),
            model.RememberMe,
            model.RememberMachine);

        if (result.Succeeded)
        {
            if (user is not null)
            {
                await MergeCartAsync(user.Id);
            }

            return RedirectToLocal(model.ReturnUrl);
        }

        ModelState.AddModelError(
            string.Empty,
            result.IsLockedOut
                ? "Your account is temporarily locked."
                : "The authenticator code is invalid.");
        return View(model);
    }

    [HttpGet("login-recovery-code")]
    public IActionResult LoginWithRecoveryCode(string? returnUrl = null) =>
        View(new RecoveryCodeLoginViewModel { ReturnUrl = returnUrl });

    [HttpPost("login-recovery-code")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> LoginWithRecoveryCode(RecoveryCodeLoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await signInManager.TwoFactorRecoveryCodeSignInAsync(
            NormalizeSecurityCode(model.RecoveryCode));

        if (result.Succeeded)
        {
            return RedirectToLocal(model.ReturnUrl);
        }

        ModelState.AddModelError(string.Empty, "The recovery code is invalid or has already been used.");
        return View(model);
    }

    [Authorize]
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("access-denied")]
    public async Task<IActionResult> AccessDenied(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true &&
            User.IsInRole(AppRoles.Admin) &&
            !User.HasClaim("amr", "mfa"))
        {
            var user = await userManager.GetUserAsync(User);
            if (user?.TwoFactorEnabled == true)
            {
                await signInManager.SignOutAsync();
                TempData["LoginMessage"] = "Administrator access requires an authenticator code. Sign in again to continue.";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            TempData["AccountError"] = "Set up two-factor authentication before entering the administration area.";
            return RedirectToAction("TwoFactor", "Customer");
        }

        return View();
    }

    [HttpGet("check-email")]
    public IActionResult CheckEmail() => View();

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(Guid userId, string token)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var decodedToken = DecodeToken(token);
        var result = await userManager.ConfirmEmailAsync(user, decodedToken);
        return View(result.Succeeded);
    }

    [HttpGet("forgot-password")]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost("forgot-password")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email.Trim());
        if (user is not null && await userManager.IsEmailConfirmedAsync(user))
        {
            await SendPasswordResetEmailAsync(user);
        }

        return RedirectToAction(nameof(CheckEmail));
    }

    [HttpGet("reset-password")]
    public IActionResult ResetPassword(string email, string token) =>
        View(new ResetPasswordViewModel { Email = email, Token = token });

    [HttpPost("reset-password")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var result = await userManager.ResetPasswordAsync(
            user,
            DecodeToken(model.Token),
            model.Password);

        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Login));
        }

        AddIdentityErrors(result);
        return View(model);
    }

    private async Task SendConfirmationEmailAsync(ApplicationUser user)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var path = Url.Action(
            nameof(ConfirmEmail),
            "Account",
            new { userId = user.Id, token = EncodeToken(token) })!;
        var link = BuildPublicUrl(path);

        await emailSender.SendAsync(
            user.Email!,
            "Confirm your Velora account",
            $"<h1>Welcome to Velora</h1><p><a href=\"{link}\">Confirm your email address</a> to activate your account.</p>");
    }

    private async Task SendPasswordResetEmailAsync(ApplicationUser user)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var path = Url.Action(
            nameof(ResetPassword),
            "Account",
            new { email = user.Email, token = EncodeToken(token) })!;
        var link = BuildPublicUrl(path);

        await emailSender.SendAsync(
            user.Email!,
            "Reset your Velora password",
            $"<p><a href=\"{link}\">Reset your password</a>. This link is intended only for you.</p>");
    }

    private async Task MergeCartAsync(Guid customerId)
    {
        var anonymousItems = cart.GetCart().Items
            .Select(item => new CustomerCartLine(
                item.ProductId,
                item.VariantId,
                item.Slug,
                item.Name,
                item.ImageUrl,
                item.Option,
                item.UnitPrice,
                item.Quantity))
            .ToList();

        await customerAccounts.MergeCartAsync(customerId, anonymousItems);
        var mergedItems = await customerAccounts.GetCartAsync(customerId);

        cart.Clear();
        foreach (var item in mergedItems)
        {
            cart.Add(new CartItem(
                item.ProductId,
                item.VariantId,
                item.Slug,
                item.Name,
                item.ImageUrl,
                item.Option,
                item.UnitPrice,
                item.Quantity));
        }
    }

    private string BuildPublicUrl(string path)
    {
        var baseUrl = new Uri(siteOptions.Value.PublicUrl.TrimEnd('/') + "/");
        return new Uri(baseUrl, path.TrimStart('/')).AbsoluteUri;
    }

    private IActionResult RedirectToLocal(string? returnUrl) =>
        LocalRedirect(returnUrl ?? Url.Action("Index", "Home")!);

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private static string EncodeToken(string token) =>
        WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    private static string DecodeToken(string token) =>
        Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

    private static string NormalizeSecurityCode(string code) =>
        code.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);
}
