using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Velora.Application.Administration;
using Velora.Infrastructure.Identity;

namespace Velora.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = AppPolicies.ManageCatalog)]
public sealed class CollectionsController(IAdminCatalogService catalog) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) => View(await catalog.GetCollectionsAsync(cancellationToken));
    [HttpGet] public IActionResult Create() => View("Edit", new AdminCollectionModel());
    [HttpGet] public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken) { var model = await catalog.GetCollectionAsync(id, cancellationToken); return model is null ? NotFound() : View(model); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Save(AdminCollectionModel model, CancellationToken cancellationToken) { if (string.IsNullOrWhiteSpace(model.Name)) ModelState.AddModelError(nameof(model.Name), "Name is required."); if (!ModelState.IsValid) return View("Edit", model); var id = await catalog.SaveCollectionAsync(model, ActorId(), IpAddress(), cancellationToken); return RedirectToAction(nameof(Edit), new { id }); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Archive(Guid id, bool archived, CancellationToken cancellationToken) { await catalog.SetCollectionArchivedAsync(id, archived, ActorId(), IpAddress(), cancellationToken); return RedirectToAction(nameof(Index)); }
    private Guid ActorId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string IpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
}
