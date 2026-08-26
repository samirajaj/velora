using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Velora.Infrastructure.Identity;
using System.Security.Claims;
using Velora.Application.Administration;

namespace Velora.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AppPolicies.ManageUsers)]
public sealed class UsersController(UserManager<ApplicationUser> userManager, IAdminCommerceService commerce) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var users = await userManager.Users.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        return View(users);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Active(Guid id, bool active)
    {
        var user = await userManager.FindByIdAsync(id.ToString()); if (user is null) return NotFound();
        if (!active && id == ActorId()) { TempData["AdminError"] = "You cannot deactivate your own administrator account."; return RedirectToAction(nameof(Index)); }
        user.IsActive = active; await userManager.UpdateSecurityStampAsync(user); await userManager.UpdateAsync(user); await commerce.RecordAuditAsync("User.ActiveChanged", nameof(ApplicationUser), id.ToString(), ActorId(), IpAddress(), new { active }); return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Role(Guid id, string role)
    {
        if (role is not (AppRoles.Admin or AppRoles.Customer)) return BadRequest(); var user = await userManager.FindByIdAsync(id.ToString()); if (user is null) return NotFound();
        if (id == ActorId() && role != AppRoles.Admin) { TempData["AdminError"] = "You cannot remove your own administrator role."; return RedirectToAction(nameof(Index)); }
        var roles = await userManager.GetRolesAsync(user); await userManager.RemoveFromRolesAsync(user, roles); await userManager.AddToRoleAsync(user, role); await userManager.UpdateSecurityStampAsync(user); await commerce.RecordAuditAsync("User.RoleChanged", nameof(ApplicationUser), id.ToString(), ActorId(), IpAddress(), new { role }); return RedirectToAction(nameof(Index));
    }
    private Guid ActorId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string IpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
}
