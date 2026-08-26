namespace Velora.Application.Commerce;

public sealed record OrderConfirmation(Guid Id, string Number, decimal Total, string Currency);
