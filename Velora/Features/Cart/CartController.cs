using Microsoft.AspNetCore.Mvc;
using Velora.Application.Catalog;
using System.Security.Claims;
using Velora.Application.Customers;

namespace Velora.Features.Cart;

public sealed class CartController(ICartService cart, IProductCatalogService catalog, ICustomerAccountService accounts) : Controller
{
    [HttpGet("cart")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true && cart.GetCart().Items.Count == 0) { var persistent = await accounts.GetCartAsync(CustomerId(), cancellationToken); foreach (var item in persistent) cart.Add(new CartItem(item.ProductId, item.VariantId, item.Slug, item.Name, item.ImageUrl, item.Option, item.UnitPrice, item.Quantity)); }
        return View(cart.GetCart());
    }

    [HttpPost("cart/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string slug, Guid variantId, int quantity = 1, CancellationToken cancellationToken = default)
    {
        var product = await catalog.GetBySlugAsync(slug, cancellationToken);
        var option = product?.Options.FirstOrDefault(x => x.Id == variantId);
        if (product is null || option is null || option.StockQuantity <= 0) return BadRequest();

        cart.Add(new CartItem(product.Id, option.Id, product.Slug, product.Name, product.ImageUrl,
            $"{option.Color} / {option.Size}", product.Price, Math.Min(quantity, option.StockQuantity)));
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
    private Guid CustomerId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private async Task PersistAsync(CancellationToken cancellationToken) { if (User.Identity?.IsAuthenticated != true) return; var items = cart.GetCart().Items.Select(x => new CustomerCartLine(x.ProductId, x.VariantId, x.Slug, x.Name, x.ImageUrl, x.Option, x.UnitPrice, x.Quantity)).ToList(); await accounts.SaveCartAsync(CustomerId(), items, cancellationToken); }
}
