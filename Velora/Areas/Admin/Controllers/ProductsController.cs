using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Velora.Application.Administration;
using Velora.Application.Media;
using Velora.Infrastructure.Identity;

namespace Velora.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AppPolicies.ManageCatalog)]
public sealed class ProductsController(IAdminCatalogService catalog, IMediaStorage media) : Controller
{
    public async Task<IActionResult> Index(string? search, bool includeArchived = false, CancellationToken cancellationToken = default) =>
        View(await catalog.GetProductsAsync(search, includeArchived, cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) => View("Edit", await catalog.CreateProductModelAsync(cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var model = await catalog.GetProductAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AdminProductModel model, CancellationToken cancellationToken)
    {
        Validate(model);
        if (!ModelState.IsValid)
        {
            var hydrated = model.Id.HasValue ? await catalog.GetProductAsync(model.Id.Value, cancellationToken) : await catalog.CreateProductModelAsync(cancellationToken);
            model.Categories = hydrated?.Categories ?? []; model.Collections = hydrated?.Collections ?? []; model.Images = hydrated?.Images ?? [];
            return View("Edit", model);
        }
        try
        {
            var id = await catalog.SaveProductAsync(model, ActorId(), IpAddress(), cancellationToken);
            TempData["AdminMessage"] = "Product saved successfully.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (Exception exception) when (exception is InvalidOperationException or Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The product could not be saved. Ensure every SKU and slug is unique.");
            var hydrated = model.Id.HasValue ? await catalog.GetProductAsync(model.Id.Value, cancellationToken) : await catalog.CreateProductModelAsync(cancellationToken);
            model.Categories = hydrated?.Categories ?? []; model.Collections = hydrated?.Collections ?? []; model.Images = hydrated?.Images ?? [];
            return View("Edit", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, bool archived, CancellationToken cancellationToken) { await catalog.SetProductArchivedAsync(id, archived, ActorId(), IpAddress(), cancellationToken); return RedirectToAction(nameof(Index), new { includeArchived = true }); }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Feature(Guid id, bool featured, CancellationToken cancellationToken) { await catalog.SetProductFeaturedAsync(id, featured, ActorId(), IpAddress(), cancellationToken); return RedirectToAction(nameof(Index)); }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(52_428_800)]
    public async Task<IActionResult> UploadImages(Guid productId, List<IFormFile> images, Guid? variantId, CancellationToken cancellationToken)
    {
        if (images.Count is 0 or > 8) TempData["AdminError"] = "Select between one and eight images.";
        else if (images.Any(x => x.Length is <= 0 or > 10_485_760 || !AllowedImageTypes.Contains(x.ContentType))) TempData["AdminError"] = "Images must be JPEG, PNG, or WebP and no larger than 10 MB each.";
        else
        {
            foreach (var image in images)
            {
                await using var stream = image.OpenReadStream();
                var upload = await media.UploadImageAsync(stream, Path.GetFileName(image.FileName), cancellationToken);
                await catalog.AddImageAsync(productId, upload, Path.GetFileNameWithoutExtension(image.FileName), variantId, ActorId(), IpAddress(), cancellationToken);
            }
            TempData["AdminMessage"] = "Images uploaded successfully.";
        }
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(Guid productId, Guid imageId, CancellationToken cancellationToken)
    {
        var image = await catalog.GetImageAsync(imageId, cancellationToken);
        if (image is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(image.PublicId)) await media.DeleteImageAsync(image.PublicId, cancellationToken);
        await catalog.DeleteImageRecordAsync(imageId, ActorId(), IpAddress(), cancellationToken);
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PrimaryImage(Guid productId, Guid imageId, CancellationToken cancellationToken) { await catalog.SetPrimaryImageAsync(productId, imageId, ActorId(), IpAddress(), cancellationToken); return RedirectToAction(nameof(Edit), new { id = productId }); }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReorderImages(Guid productId, List<Guid> imageIds, CancellationToken cancellationToken) { await catalog.ReorderImagesAsync(productId, imageIds, ActorId(), IpAddress(), cancellationToken); return RedirectToAction(nameof(Edit), new { id = productId }); }

    private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };
    private Guid ActorId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string IpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
    private void Validate(AdminProductModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name)) ModelState.AddModelError(nameof(model.Name), "Name is required.");
        if (model.Name.Length > 200) ModelState.AddModelError(nameof(model.Name), "Name cannot exceed 200 characters.");
        if (model.Price <= 0) ModelState.AddModelError(nameof(model.Price), "Price must be greater than zero.");
        if (model.CompareAtPrice.HasValue && model.CompareAtPrice <= model.Price) ModelState.AddModelError(nameof(model.CompareAtPrice), "Sale comparison price must be higher than the selling price.");
        if (model.CategoryId == Guid.Empty) ModelState.AddModelError(nameof(model.CategoryId), "Category is required.");
        if (model.Variants.Count == 0) ModelState.AddModelError(nameof(model.Variants), "At least one variant is required.");
        if (model.Variants.Any(x => string.IsNullOrWhiteSpace(x.Sku) || string.IsNullOrWhiteSpace(x.Size) || string.IsNullOrWhiteSpace(x.Color))) ModelState.AddModelError(nameof(model.Variants), "Every variant requires an SKU, color, and size.");
        if (model.Variants.Any(x => x.StockQuantity < 0 || x.LowStockThreshold < 0)) ModelState.AddModelError(nameof(model.Variants), "Inventory values cannot be negative.");
        if (model.Variants.Where(x => !string.IsNullOrWhiteSpace(x.Sku)).GroupBy(x => x.Sku.Trim(), StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1)) ModelState.AddModelError(nameof(model.Variants), "SKUs must be unique.");
    }
}
