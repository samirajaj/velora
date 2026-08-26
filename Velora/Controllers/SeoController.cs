using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Velora.Application.Catalog;
using Velora.Configuration;

namespace Velora.Controllers;

public sealed class SeoController(
    IProductCatalogService catalog,
    IOptions<SiteOptions> siteOptions) : Controller
{
    [HttpGet("robots.txt")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult Robots() => Content(
        $"User-agent: *\nAllow: /\nDisallow: /admin\nSitemap: {BuildPublicUrl("sitemap.xml")}\n",
        "text/plain");

    [HttpGet("sitemap.xml")]
    [ResponseCache(Duration = 900, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var root = new XElement(ns + "urlset",
            UrlElement(ns, BuildPublicUrl(Url.Action("Index", "Home")!)),
            UrlElement(ns, BuildPublicUrl(Url.Action("Index", "Product")!)));
        for (var page = 1; page <= 50; page++)
        {
            var result = await catalog.BrowseAsync(new CatalogRequest(Page: page, PageSize: 100, Sort: "newest"), cancellationToken);
            foreach (var product in result.Items)
                root.Add(UrlElement(ns, BuildPublicUrl(Url.Action("Details", "Product", new { product.Slug })!)));
            if (page >= result.TotalPages) break;
        }
        return Content(new XDocument(new XDeclaration("1.0", "utf-8", null), root).ToString(), "application/xml");
    }

    private static XElement UrlElement(XNamespace ns, string location) => new(ns + "url", new XElement(ns + "loc", location));

    private string BuildPublicUrl(string path)
    {
        var baseUri = new Uri(siteOptions.Value.PublicUrl.TrimEnd('/') + "/");
        return new Uri(baseUri, path.TrimStart('/')).AbsoluteUri;
    }
}
