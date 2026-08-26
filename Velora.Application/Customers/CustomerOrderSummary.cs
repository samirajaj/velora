namespace Velora.Application.Customers;

public sealed record CustomerOrderSummary(Guid Id, string Number, string Status, decimal Total, string Currency, DateTime CreatedAtUtc, int ItemCount);
