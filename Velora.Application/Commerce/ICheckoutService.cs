namespace Velora.Application.Commerce;

public interface ICheckoutService
{
    Task<CheckoutQuote> QuoteAsync(IReadOnlyList<CheckoutLine> items, string? couponCode, CancellationToken cancellationToken = default);
    Task<OrderConfirmation> PlaceCashOnDeliveryOrderAsync(CheckoutRequest request, CancellationToken cancellationToken = default);
    Task<OrderDetailsModel?> GetOrderAsync(Guid orderId, Guid customerId, CancellationToken cancellationToken = default);
}
