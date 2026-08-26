namespace Velora.Application.Customers;

public sealed record CustomerCartLine(Guid ProductId, Guid VariantId, string Slug, string Name, string ImageUrl, string Option, decimal UnitPrice, int Quantity);
