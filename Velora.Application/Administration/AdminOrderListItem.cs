namespace Velora.Application.Administration;

public sealed record AdminOrderListItem(Guid Id, string Number, string CustomerEmail, string Status, decimal Total, string Currency, int ItemCount, DateTime CreatedAtUtc);
