namespace Velora.Application.Administration;

public sealed record AdminOrderLine(string ProductName, string Sku, string Option, int Quantity, decimal LineTotal);
