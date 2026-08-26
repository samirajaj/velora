using Microsoft.AspNetCore.Mvc;
using Velora.Application.Catalog;

namespace Velora.Features.Cart;

public sealed class CartController(ICartService cart, IProductCatalogService catalog) : Controller
{
    [HttpGet("cart")]
    public IActionResult Index() => View(cart.GetCart());

    [HttpPost("cart/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string slug, Guid variantId, int quantity = 1, CancellationToken cancellationToken = default)
    {
        var product = await catalog.GetBySlugAsync(slug, cancellationToken);
        var option = product?.Options.FirstOrDefault(x => x.Id == variantId);
        if (product is null || option is null || option.StockQuantity <= 0) return BadRequest();

        cart.Add(new CartItem(product.Id, option.Id, product.Slug, product.Name, product.ImageUrl,
            $"{option.Color} / {option.Size}", product.Price, Math.Min(quantity, option.StockQuantity)));
        TempData["CartMessage"] = $"{product.Name} was added to your bag.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("cart/update")]
    [ValidateAntiForgeryToken]
    public IActionResult Update(Guid variantId, int quantity)
    {
        cart.Update(variantId, quantity);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("cart/remove")]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(Guid variantId)
    {
        cart.Remove(variantId);
        return RedirectToAction(nameof(Index));
    }
}
