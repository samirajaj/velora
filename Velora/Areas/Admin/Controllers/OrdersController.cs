using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Velora.Application.Administration;
using Velora.Infrastructure.Identity;

namespace Velora.Areas.Admin.Controllers;
[Area("Admin"), Authorize(Policy = AppPolicies.ManageOrders)]
public sealed class OrdersController(IAdminCommerceService commerce) : Controller
{
    public async Task<IActionResult> Index(string? status, int page = 1, CancellationToken cancellationToken = default) => View(await commerce.GetOrdersAsync(status, page, 30, cancellationToken));
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken) { var model = await commerce.GetOrderAsync(id, cancellationToken); return model is null ? NotFound() : View(model); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Update(Guid id, string status, string shipmentStatus, string note, CancellationToken cancellationToken) { await commerce.UpdateOrderAsync(id, status, shipmentStatus, note, ActorId(), IpAddress(), cancellationToken); TempData["AdminMessage"] = "Order updated."; return RedirectToAction(nameof(Details), new { id }); }
    private Guid ActorId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); private string IpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
}
