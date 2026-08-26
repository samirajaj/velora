namespace Velora.Domain.Commerce;

public enum OrderStatus { Pending, Confirmed, Processing, Shipped, Delivered, Cancelled }
public enum PaymentStatus { Pending, DueOnDelivery, Paid, Failed, Refunded, Cancelled }
public enum ShipmentStatus { Pending, Preparing, Shipped, Delivered, Returned }

public sealed class Order
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal DeliveryTotal { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "USD";
    public string? CouponCode { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string StateOrProvince { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string CustomerNote { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<OrderItem> Items { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
    public ICollection<Shipment> Shipments { get; set; } = [];
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = [];
}
