namespace Velora.Application.Commerce;

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
