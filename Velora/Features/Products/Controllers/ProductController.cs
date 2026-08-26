using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Velora.Application.Catalog;

namespace Velora.Features.Products.Controllers;

public sealed class ProductController(IProductCatalogService catalog) : Controller
{
    [HttpGet("shop")]
    [OutputCache(PolicyName = "Storefront")]
    public async Task<IActionResult> Index(
        string? category,
        string? search,
        string sort = "featured",
        int page = 1,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? color = null,
        string? size = null,
        bool inStockOnly = false,
        CancellationToken cancellationToken = default)
    {
        var request = new CatalogRequest(
            category,
            search,
            sort,
            page,
            PageSize: 12,
            minPrice,
            maxPrice,
            color,
            size,
            inStockOnly);

        var model = await catalog.BrowseAsync(request, cancellationToken);
        PopulateFilterViewData(request);
        return View(model);
    }

    [HttpGet("shop/{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        var product = await catalog.GetBySlugAsync(slug, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        PopulateSeoViewData(product);
        RememberRecentlyViewed(product.Slug);
        return View(product);
    }

    [HttpGet("shop/suggestions")]
    public async Task<IActionResult> Suggestions(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return Json(Array.Empty<object>());
        }

        var result = await catalog.BrowseAsync(
            new CatalogRequest(Search: query, PageSize: 6),
            cancellationToken);

        return Json(result.Items.Select(product => new
        {
            product.Name,
            product.Slug,
            product.ImageUrl,
            product.Price
        }));
    }

    private void PopulateFilterViewData(CatalogRequest request)
    {
        ViewData["Category"] = request.Category;
        ViewData["Search"] = request.Search;
        ViewData["Sort"] = request.Sort;
        ViewData["MinPrice"] = request.MinPrice;
        ViewData["MaxPrice"] = request.MaxPrice;
        ViewData["Color"] = request.Color;
        ViewData["Size"] = request.Size;
        ViewData["InStockOnly"] = request.InStockOnly;
    }

    private void PopulateSeoViewData(ProductDetails product)
    {
        ViewData["Title"] = string.IsNullOrWhiteSpace(product.SeoTitle) ? product.Name : product.SeoTitle;
        ViewData["Description"] = string.IsNullOrWhiteSpace(product.SeoDescription)
            ? product.Description
            : product.SeoDescription;
        ViewData["Image"] = product.Images.FirstOrDefault() ?? product.ImageUrl;
    }

    private void RememberRecentlyViewed(string slug)
    {
        const string sessionKey = "recent-products";
        var recent = HttpContext.Session.GetString(sessionKey)?
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .ToList() ?? [];

        recent.Remove(slug);
        recent.Insert(0, slug);
        HttpContext.Session.SetString(sessionKey, string.Join(',', recent.Take(8)));
    }
}
