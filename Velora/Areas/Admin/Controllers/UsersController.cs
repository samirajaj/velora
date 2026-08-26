using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Velora.Infrastructure.Identity;

namespace Velora.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class UsersController(UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var users = await userManager.Users.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        return View(users);
    }
}
