namespace Velora.Domain.Commerce;

public enum PaymentStatus
{
    Pending,
    DueOnDelivery,
    Paid,
    Failed,
    Refunded,
    Cancelled
}
