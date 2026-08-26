using System.ComponentModel.DataAnnotations;
using Velora.Application.Commerce;
using Velora.Features.Cart;

namespace Velora.Features.Checkout;

public sealed class CheckoutFormViewModel
{
    [Required, EmailAddress, StringLength(256)]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required, StringLength(160)]
    public string RecipientName { get; set; } = string.Empty;

    [Required, Phone, StringLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(200)]
    public string AddressLine2 { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [StringLength(100)]
    public string StateOrProvince { get; set; } = string.Empty;

    [StringLength(30)]
    public string PostalCode { get; set; } = string.Empty;

    [Required, StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; set; } = "SY";

    [StringLength(1_000)]
    public string CustomerNote { get; set; } = string.Empty;

    [StringLength(50)]
    public string? CouponCode { get; set; }

    public CartViewModel Cart { get; set; } = new([]);
    public CheckoutQuote Quote { get; set; } = new(0, 0, 0, 0, "USD");
}
