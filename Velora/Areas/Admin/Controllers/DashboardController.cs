using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Velora.Infrastructure.Identity;
using Velora.Application.Administration;

namespace Velora.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AppPolicies.ViewAudit)]
public sealed class DashboardController(IAdminCommerceService commerce) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(await commerce.GetDashboardAsync(cancellationToken));
}
