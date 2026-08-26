namespace Velora.Application.Commerce;

public sealed record CheckoutLine(Guid ProductId, Guid VariantId, int Quantity);
