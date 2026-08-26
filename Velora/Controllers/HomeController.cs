using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Velora.Application.Catalog;
using Velora.Models;

namespace Velora.Controllers;

public class HomeController(IProductCatalogService catalog) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new HomeViewModel(
            await catalog.GetFeaturedAsync(4, cancellationToken),
            await catalog.GetCategoriesAsync(cancellationToken));
        return View(model);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
    });
}
