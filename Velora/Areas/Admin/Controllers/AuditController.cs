using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Velora.Application.Administration;
using Velora.Infrastructure.Identity;
namespace Velora.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Policy = AppPolicies.ViewAudit)]
public sealed class AuditController(IAdminCommerceService commerce) : Controller { public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default) => View(await commerce.GetAuditAsync(page, 50, cancellationToken)); }
