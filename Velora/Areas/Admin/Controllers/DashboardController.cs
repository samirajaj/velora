using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Velora.Infrastructure.Identity;

namespace Velora.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class DashboardController : Controller
{
    public IActionResult Index() => View();
}
