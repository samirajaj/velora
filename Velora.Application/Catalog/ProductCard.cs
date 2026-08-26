namespace Velora.Application.Catalog;

public sealed record ProductCard(Guid Id, string Name, string Slug, decimal Price, decimal? CompareAtPrice, string ImageUrl, string CategoryName, bool IsNew);
