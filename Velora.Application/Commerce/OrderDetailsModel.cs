namespace Velora.Application.Commerce;

public sealed record OrderDetailsModel(
    Guid Id,
    string Number,
    string Status,
    decimal Subtotal,
    decimal Discount,
    decimal Delivery,
    decimal Total,
    string Currency,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderLineModel> Items,
    string Address,
    string PaymentStatus);
