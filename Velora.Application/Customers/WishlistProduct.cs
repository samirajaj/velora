namespace Velora.Application.Customers;

public sealed record WishlistProduct(Guid ProductId, string Name, string Slug, decimal Price, string ImageUrl);
