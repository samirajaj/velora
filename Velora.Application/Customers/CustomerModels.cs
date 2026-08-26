namespace Velora.Application.Customers;

public sealed class CustomerProfile
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

public sealed class CustomerAddress
{
    public Guid? Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Line1 { get; set; } = string.Empty;
    public string Line2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string StateOrProvince { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "SY";
    public bool IsDefault { get; set; }
}

public sealed record CustomerCartLine(Guid ProductId, Guid VariantId, string Slug, string Name, string ImageUrl, string Option, decimal UnitPrice, int Quantity);
public sealed record WishlistProduct(Guid ProductId, string Name, string Slug, decimal Price, string ImageUrl);
public sealed record CustomerOrderSummary(Guid Id, string Number, string Status, decimal Total, string Currency, DateTime CreatedAtUtc, int ItemCount);

public interface ICustomerAccountService
{
    Task<CustomerProfile?> GetProfileAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task UpdateProfileAsync(Guid customerId, CustomerProfile model, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerAddress>> GetAddressesAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<CustomerAddress?> GetAddressAsync(Guid customerId, Guid id, CancellationToken cancellationToken = default);
    Task<Guid> SaveAddressAsync(Guid customerId, CustomerAddress model, CancellationToken cancellationToken = default);
    Task ArchiveAddressAsync(Guid customerId, Guid id, CancellationToken cancellationToken = default);
    Task MergeCartAsync(Guid customerId, IReadOnlyList<CustomerCartLine> anonymousItems, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerCartLine>> GetCartAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task SaveCartAsync(Guid customerId, IReadOnlyList<CustomerCartLine> items, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WishlistProduct>> GetWishlistAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<bool> ToggleWishlistAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerOrderSummary>> GetOrdersAsync(Guid customerId, CancellationToken cancellationToken = default);
}
