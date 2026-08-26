using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Velora.Application.Catalog;

namespace Velora.Controllers;

public sealed class SeoController(IProductCatalogService catalog) : Controller
{
    [HttpGet("robots.txt")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult Robots() => Content($"User-agent: *\nAllow: /\nDisallow: /admin\nSitemap: {Request.Scheme}://{Request.Host}/sitemap.xml\n", "text/plain");

    [HttpGet("sitemap.xml")]
    [ResponseCache(Duration = 900, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var root = new XElement(ns + "urlset",
            UrlElement(ns, Url.Action("Index", "Home", null, Request.Scheme)!),
            UrlElement(ns, Url.Action("Index", "Product", null, Request.Scheme)!));
        for (var page = 1; page <= 50; page++)
        {
            var result = await catalog.BrowseAsync(new CatalogRequest(Page: page, PageSize: 100, Sort: "newest"), cancellationToken);
            foreach (var product in result.Items)
                root.Add(UrlElement(ns, Url.Action("Details", "Product", new { product.Slug }, Request.Scheme)!));
            if (page >= result.TotalPages) break;
        }
        return Content(new XDocument(new XDeclaration("1.0", "utf-8", null), root).ToString(), "application/xml");
    }

    private static XElement UrlElement(XNamespace ns, string location) => new(ns + "url", new XElement(ns + "loc", location));
}
