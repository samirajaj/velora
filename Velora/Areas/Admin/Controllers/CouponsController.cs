using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Velora.Application.Administration;
using Velora.Infrastructure.Identity;

namespace Velora.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Policy = AppPolicies.ManageCatalog)]
public sealed class CouponsController(IAdminCommerceService commerce) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(await commerce.GetCouponsAsync(cancellationToken));
    [HttpGet] public IActionResult Create() => View("Edit", new AdminCouponModel());
    [HttpGet] public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken) { var model = await commerce.GetCouponAsync(id, cancellationToken); return model is null ? NotFound() : View(model); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Save(AdminCouponModel model, CancellationToken cancellationToken) { if (string.IsNullOrWhiteSpace(model.Code) || model.Value <= 0) { ModelState.AddModelError(string.Empty, "Code and a positive value are required."); return View("Edit", model); } var id = await commerce.SaveCouponAsync(model, ActorId(), IpAddress(), cancellationToken); return RedirectToAction(nameof(Edit), new { id }); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken) { await commerce.ArchiveCouponAsync(id, ActorId(), IpAddress(), cancellationToken); return RedirectToAction(nameof(Index)); }
    private Guid ActorId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); private string IpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
}
