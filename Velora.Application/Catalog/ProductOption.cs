namespace Velora.Application.Catalog;

public sealed record ProductOption(Guid Id, string Sku, string Color, string ColorHex, string Size, int StockQuantity);
