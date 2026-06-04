namespace Velora.Features.Products.ViewModels;

public class ProductListItemVm
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
}
