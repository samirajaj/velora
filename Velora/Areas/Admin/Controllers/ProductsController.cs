using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Velora.Application.Administration;
using Velora.Application.Media;
using Velora.Features.Media;
using Velora.Infrastructure.Identity;

namespace Velora.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AppPolicies.ManageCatalog)]
public sealed class ProductsController(
    IAdminCatalogService catalog,
    IMediaStorage media,
    IProductImageValidator imageValidator) : AdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        bool includeArchived = false,
        CancellationToken cancellationToken = default) =>
        View(await catalog.GetProductsAsync(
            search,
            includeArchived,
            cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        View("Edit", await catalog.CreateProductModelAsync(cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var model = await catalog.GetProductAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        AdminProductModel model,
        CancellationToken cancellationToken)
    {
        ValidateProduct(model);
        if (!ModelState.IsValid)
        {
            await HydrateProductModelAsync(model, cancellationToken);
            return View("Edit", model);
        }

        try
        {
            var id = await catalog.SaveProductAsync(
                model,
                ActorId,
                IpAddress,
                cancellationToken);
            TempData["AdminMessage"] = "Product saved successfully.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The product could not be saved. Ensure every SKU and slug is unique.");
            await HydrateProductModelAsync(model, cancellationToken);
            return View("Edit", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(
        Guid id,
        bool archived,
        CancellationToken cancellationToken)
    {
        await catalog.SetProductArchivedAsync(
            id,
            archived,
            ActorId,
            IpAddress,
            cancellationToken);
        return RedirectToAction(nameof(Index), new { includeArchived = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Feature(
        Guid id,
        bool featured,
        CancellationToken cancellationToken)
    {
        await catalog.SetProductFeaturedAsync(
            id,
            featured,
            ActorId,
            IpAddress,
            cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(52_428_800)]
    public async Task<IActionResult> UploadImages(
        Guid productId,
        List<IFormFile> images,
        Guid? variantId,
        CancellationToken cancellationToken)
    {
        if (images.Count is 0 or > ProductImageValidator.MaximumFileCount)
        {
            TempData["AdminError"] = "Select between one and eight images.";
            return RedirectToEdit(productId);
        }

        foreach (var image in images)
        {
            if (!await imageValidator.IsValidAsync(image, cancellationToken))
            {
                TempData["AdminError"] = "Images must be valid JPEG, PNG, or WebP files no larger than 10 MB each.";
                return RedirectToEdit(productId);
            }
        }

        foreach (var image in images)
        {
            await UploadImageAsync(productId, variantId, image, cancellationToken);
        }

        TempData["AdminMessage"] = "Images uploaded successfully.";
        return RedirectToEdit(productId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        var image = await catalog.GetImageAsync(imageId, cancellationToken);
        if (image is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(image.PublicId))
        {
            await media.DeleteImageAsync(image.PublicId, cancellationToken);
        }

        await catalog.DeleteImageRecordAsync(
            imageId,
            ActorId,
            IpAddress,
            cancellationToken);
        return RedirectToEdit(productId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PrimaryImage(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        await catalog.SetPrimaryImageAsync(
            productId,
            imageId,
            ActorId,
            IpAddress,
            cancellationToken);
        return RedirectToEdit(productId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReorderImages(
        Guid productId,
        List<Guid> imageIds,
        CancellationToken cancellationToken)
    {
        await catalog.ReorderImagesAsync(
            productId,
            imageIds,
            ActorId,
            IpAddress,
            cancellationToken);
        return RedirectToEdit(productId);
    }

    private async Task UploadImageAsync(
        Guid productId,
        Guid? variantId,
        IFormFile image,
        CancellationToken cancellationToken)
    {
        var safeFileName = Path.GetFileName(image.FileName);
        await using var stream = image.OpenReadStream();
        var upload = await media.UploadImageAsync(
            stream,
            safeFileName,
            cancellationToken);

        await catalog.AddImageAsync(
            productId,
            upload,
            Path.GetFileNameWithoutExtension(safeFileName),
            variantId,
            ActorId,
            IpAddress,
            cancellationToken);
    }

    private async Task HydrateProductModelAsync(
        AdminProductModel model,
        CancellationToken cancellationToken)
    {
        var hydrated = model.Id.HasValue
            ? await catalog.GetProductAsync(model.Id.Value, cancellationToken)
            : await catalog.CreateProductModelAsync(cancellationToken);

        model.Categories = hydrated?.Categories ?? [];
        model.Collections = hydrated?.Collections ?? [];
        model.Images = hydrated?.Images ?? [];
    }

    private RedirectToActionResult RedirectToEdit(Guid productId) =>
        RedirectToAction(nameof(Edit), new { id = productId });

    private void ValidateProduct(AdminProductModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(nameof(model.Name), "Name is required.");
        }
        else if (model.Name.Length > 200)
        {
            ModelState.AddModelError(nameof(model.Name), "Name cannot exceed 200 characters.");
        }

        if (model.Price <= 0)
        {
            ModelState.AddModelError(nameof(model.Price), "Price must be greater than zero.");
        }

        if (model.CompareAtPrice.HasValue && model.CompareAtPrice <= model.Price)
        {
            ModelState.AddModelError(
                nameof(model.CompareAtPrice),
                "Sale comparison price must be higher than the selling price.");
        }

        if (model.CategoryId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(model.CategoryId), "Category is required.");
        }

        ValidateVariants(model.Variants);
    }

    private void ValidateVariants(IReadOnlyCollection<AdminVariantModel> variants)
    {
        if (variants.Count == 0)
        {
            ModelState.AddModelError(nameof(AdminProductModel.Variants), "At least one variant is required.");
            return;
        }

        if (variants.Any(variant =>
                string.IsNullOrWhiteSpace(variant.Sku) ||
                string.IsNullOrWhiteSpace(variant.Size) ||
                string.IsNullOrWhiteSpace(variant.Color)))
        {
            ModelState.AddModelError(
                nameof(AdminProductModel.Variants),
                "Every variant requires an SKU, color, and size.");
        }

        if (variants.Any(variant =>
                variant.StockQuantity < 0 || variant.LowStockThreshold < 0))
        {
            ModelState.AddModelError(
                nameof(AdminProductModel.Variants),
                "Inventory values cannot be negative.");
        }

        var hasDuplicateSku = variants
            .Where(variant => !string.IsNullOrWhiteSpace(variant.Sku))
            .GroupBy(variant => variant.Sku.Trim(), StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1);

        if (hasDuplicateSku)
        {
            ModelState.AddModelError(nameof(AdminProductModel.Variants), "SKUs must be unique.");
        }
    }
}
