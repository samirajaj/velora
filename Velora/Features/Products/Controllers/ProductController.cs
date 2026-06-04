using Microsoft.AspNetCore.Mvc;
using Velora.Features.Products.Services;

namespace Velora.Features.Products.Controllers;

public class ProductController(ProductService productService) : Controller
{
    private readonly ProductService _productService = productService;

    public async Task<IActionResult> Index()
    {
        var products = await _productService.GetAllAsync();

        return View(products);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var product = await _productService.GetByIdAsync(id);

        if (product is null)
            return NotFound();

        return View(product);
    }
}
