namespace Velora.Domain.Commerce;

public sealed class Payment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public string Method { get; set; } = "CashOnDelivery";
    public PaymentStatus Status { get; set; } = PaymentStatus.DueOnDelivery;
    public decimal Amount { get; set; }
    public string? ExternalReference { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAtUtc { get; set; }
}
