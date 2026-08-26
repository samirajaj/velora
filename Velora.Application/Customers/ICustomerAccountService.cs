namespace Velora.Application.Customers;

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
