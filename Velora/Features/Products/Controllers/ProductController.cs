using Microsoft.AspNetCore.Mvc;
using Velora.Application.Catalog;

namespace Velora.Features.Products.Controllers;

public class ProductController(IProductCatalogService catalog) : Controller
{
    [HttpGet("shop")]
    public async Task<IActionResult> Index(string? category, string? search, string sort = "featured", int page = 1, CancellationToken cancellationToken = default)
    {
        var model = await catalog.BrowseAsync(new CatalogRequest(category, search, sort, page), cancellationToken);
        ViewData["Category"] = category;
        ViewData["Search"] = search;
        ViewData["Sort"] = sort;
        return View(model);
    }

    [HttpGet("shop/{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        var product = await catalog.GetBySlugAsync(slug, cancellationToken);
        if (product is null) return NotFound();
        return View(product);
    }
}
