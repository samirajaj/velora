using Microsoft.AspNetCore.Mvc;
using Velora.Application.Catalog;
using Microsoft.AspNetCore.OutputCaching;

namespace Velora.Features.Products.Controllers;

public class ProductController(IProductCatalogService catalog) : Controller
{
    [HttpGet("shop")]
    [OutputCache(PolicyName = "Storefront")]
    public async Task<IActionResult> Index(string? category, string? search, string sort = "featured", int page = 1, decimal? minPrice = null, decimal? maxPrice = null, string? color = null, string? size = null, bool inStockOnly = false, CancellationToken cancellationToken = default)
    {
        var model = await catalog.BrowseAsync(new CatalogRequest(category, search, sort, page, 12, minPrice, maxPrice, color, size, inStockOnly), cancellationToken);
        ViewData["Category"] = category;
        ViewData["Search"] = search;
        ViewData["Sort"] = sort;
        ViewData["MinPrice"] = minPrice;
        ViewData["MaxPrice"] = maxPrice;
        ViewData["Color"] = color;
        ViewData["Size"] = size;
        ViewData["InStockOnly"] = inStockOnly;
        return View(model);
    }

    [HttpGet("shop/{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        var product = await catalog.GetBySlugAsync(slug, cancellationToken);
        if (product is null) return NotFound();
        ViewData["Title"] = string.IsNullOrWhiteSpace(product.SeoTitle) ? product.Name : product.SeoTitle;
        ViewData["Description"] = string.IsNullOrWhiteSpace(product.SeoDescription) ? product.Description : product.SeoDescription;
        ViewData["Image"] = product.Images.FirstOrDefault() ?? product.ImageUrl;
        var recent = HttpContext.Session.GetString("recent-products")?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? [];
        recent.Remove(product.Slug); recent.Insert(0, product.Slug); HttpContext.Session.SetString("recent-products", string.Join(',', recent.Take(8)));
        return View(product);
    }

    [HttpGet("shop/suggestions")]
    public async Task<IActionResult> Suggestions(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return Json(Array.Empty<object>());
        var result = await catalog.BrowseAsync(new CatalogRequest(Search: query, PageSize: 6), cancellationToken);
        return Json(result.Items.Select(x => new { x.Name, x.Slug, x.ImageUrl, x.Price }));
    }
}
