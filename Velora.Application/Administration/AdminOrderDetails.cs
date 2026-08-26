namespace Velora.Application.Administration;

public sealed record AdminOrderDetails(
    Guid Id,
    string Number,
    string CustomerEmail,
    string RecipientName,
    string Phone,
    string Address,
    string Status,
    string PaymentStatus,
    string ShipmentStatus,
    decimal Total,
    string Currency,
    DateTime CreatedAtUtc,
    IReadOnlyList<AdminOrderLine> Items,
    IReadOnlyList<string> History);
