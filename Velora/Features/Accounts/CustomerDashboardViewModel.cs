using Velora.Application.Customers;

namespace Velora.Features.Accounts;

public sealed record CustomerDashboardViewModel(
    CustomerProfile? Profile,
    IReadOnlyList<CustomerAddress> Addresses,
    IReadOnlyList<CustomerOrderSummary> Orders,
    IReadOnlyList<WishlistProduct> Wishlist);
