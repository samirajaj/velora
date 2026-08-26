using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Velora.Infrastructure.Identity;

namespace Velora.Features.Accounts;

[Route("account")]
public sealed class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : Controller
{
    [HttpGet("register")]
    public IActionResult Register(string? returnUrl = null) => View(new RegisterViewModel());

    [HttpPost("register")]
    [ValidateAntiForgeryToken]
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
            await signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl ?? Url.Action("Index", "Home")!);
        }

        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        return View(model);
    }

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await signInManager.PasswordSignInAsync(model.Email.Trim(), model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded) return LocalRedirect(model.ReturnUrl ?? Url.Action("Index", "Home")!);
        ModelState.AddModelError(string.Empty, result.IsLockedOut ? "Your account is temporarily locked. Please try again later." : "The email or password is incorrect.");
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
}
