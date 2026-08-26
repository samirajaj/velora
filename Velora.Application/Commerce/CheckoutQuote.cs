namespace Velora.Application.Commerce;

public sealed record CheckoutQuote(decimal Subtotal, decimal Discount, decimal Delivery, decimal Total, string Currency);
