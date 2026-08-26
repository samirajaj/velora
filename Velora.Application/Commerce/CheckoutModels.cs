namespace Velora.Application.Commerce;

public sealed record CheckoutLine(Guid ProductId, Guid VariantId, int Quantity);

public sealed class CheckoutRequest
{
    public Guid CustomerId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string StateOrProvince { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "SY";
    public string CustomerNote { get; set; } = string.Empty;
    public string? CouponCode { get; set; }
    public IReadOnlyList<CheckoutLine> Items { get; set; } = [];
}

public sealed record CheckoutQuote(decimal Subtotal, decimal Discount, decimal Delivery, decimal Total, string Currency);
public sealed record OrderConfirmation(Guid Id, string Number, decimal Total, string Currency);
public sealed record OrderDetailsModel(Guid Id, string Number, string Status, decimal Subtotal, decimal Discount, decimal Delivery, decimal Total, string Currency, DateTime CreatedAtUtc, IReadOnlyList<OrderLineModel> Items, string Address, string PaymentStatus);
public sealed record OrderLineModel(string ProductName, string Option, string Sku, string ImageUrl, decimal UnitPrice, int Quantity, decimal LineTotal);

public interface ICheckoutService
{
    Task<CheckoutQuote> QuoteAsync(IReadOnlyList<CheckoutLine> items, string? couponCode, CancellationToken cancellationToken = default);
    Task<OrderConfirmation> PlaceCashOnDeliveryOrderAsync(CheckoutRequest request, CancellationToken cancellationToken = default);
    Task<OrderDetailsModel?> GetOrderAsync(Guid orderId, Guid customerId, CancellationToken cancellationToken = default);
}
