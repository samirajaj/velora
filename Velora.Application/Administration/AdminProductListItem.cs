namespace Velora.Application.Administration;

public sealed record AdminProductListItem(Guid Id, string Name, string Slug, string Category, decimal Price, bool IsFeatured, bool IsActive, bool IsArchived, int Stock, string ImageUrl);
