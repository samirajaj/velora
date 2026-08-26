namespace Velora.Application.Commerce;

public sealed record OrderLineModel(string ProductName, string Option, string Sku, string ImageUrl, decimal UnitPrice, int Quantity, decimal LineTotal);
