using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Velora.Areas.Admin.Controllers;

public abstract class AdminControllerBase : Controller
{
    protected Guid ActorId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    protected string IpAddress =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
}
