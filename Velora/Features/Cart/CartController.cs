using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Velora.Application.Catalog;
using Velora.Application.Customers;

namespace Velora.Features.Cart;

public sealed class CartController(
    ICartService cart,
    IProductCatalogService catalog,
    ICustomerAccountService accounts) : Controller
{
    [HttpGet("cart")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        await RestorePersistentCartAsync(cancellationToken);
        return View(cart.GetCart());
    }

    [HttpPost("cart/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string slug, Guid variantId, int quantity = 1, CancellationToken cancellationToken = default)
    {
        var product = await catalog.GetBySlugAsync(slug, cancellationToken);
        var option = product?.Options.FirstOrDefault(x => x.Id == variantId);
        if (product is null || option is null || option.StockQuantity <= 0)
        {
            return BadRequest();
        }

        cart.Add(new CartItem(
            product.Id,
            option.Id,
            product.Slug,
            product.Name,
            product.ImageUrl,
            $"{option.Color} / {option.Size}",
            product.Price,
            Math.Clamp(quantity, 1, option.StockQuantity)));
        await PersistAsync(cancellationToken);
        TempData["CartMessage"] = $"{product.Name} was added to your bag.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("cart/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid variantId, int quantity, CancellationToken cancellationToken)
    {
        cart.Update(variantId, quantity);
        await PersistAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("cart/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid variantId, CancellationToken cancellationToken)
    {
        cart.Remove(variantId);
        await PersistAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }
    private async Task RestorePersistentCartAsync(CancellationToken cancellationToken)
    {
        if (!IsAuthenticated() || cart.GetCart().Items.Count > 0)
        {
            return;
        }

        var persistentItems = await accounts.GetCartAsync(CustomerId(), cancellationToken);
        foreach (var item in persistentItems)
        {
            cart.Add(new CartItem(
                item.ProductId,
                item.VariantId,
                item.Slug,
                item.Name,
                item.ImageUrl,
                item.Option,
                item.UnitPrice,
                item.Quantity));
        }
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        if (!IsAuthenticated())
        {
            return;
        }

        var items = cart.GetCart().Items
            .Select(item => new CustomerCartLine(
                item.ProductId,
                item.VariantId,
                item.Slug,
                item.Name,
                item.ImageUrl,
                item.Option,
                item.UnitPrice,
                item.Quantity))
            .ToList();

        await accounts.SaveCartAsync(CustomerId(), items, cancellationToken);
    }

    private bool IsAuthenticated() => User.Identity?.IsAuthenticated == true;

    private Guid CustomerId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
