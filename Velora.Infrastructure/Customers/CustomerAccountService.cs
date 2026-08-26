using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Velora.Application.Customers;
using Velora.Domain.Customers;
using Velora.Infrastructure.Persistence;

namespace Velora.Infrastructure.Customers;

internal sealed class CustomerAccountService(ApplicationDbContext context) : ICustomerAccountService
{
    public Task<CustomerProfile?> GetProfileAsync(
        Guid customerId,
        CancellationToken cancellationToken = default) =>
        context.Users
            .AsNoTracking()
            .Where(user => user.Id == customerId)
            .Select(user => new CustomerProfile
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty
            })
            .FirstOrDefaultAsync(cancellationToken);

    public async Task UpdateProfileAsync(
        Guid customerId,
        CustomerProfile model,
        CancellationToken cancellationToken = default)
    {
        var user = await context.Users.FindAsync([customerId], cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        user.PhoneNumber = model.PhoneNumber.Trim();
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerAddress>> GetAddressesAsync(
        Guid customerId,
        CancellationToken cancellationToken = default) =>
        await context.Addresses
            .AsNoTracking()
            .Where(address => address.CustomerId == customerId && !address.IsArchived)
            .OrderByDescending(address => address.IsDefault)
            .Select(AddressProjection)
            .ToListAsync(cancellationToken);

    public Task<CustomerAddress?> GetAddressAsync(
        Guid customerId,
        Guid id,
        CancellationToken cancellationToken = default) =>
        context.Addresses
            .AsNoTracking()
            .Where(address =>
                address.CustomerId == customerId &&
                address.Id == id &&
                !address.IsArchived)
            .Select(AddressProjection)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Guid> SaveAddressAsync(
        Guid customerId,
        CustomerAddress model,
        CancellationToken cancellationToken = default)
    {
        var address = await FindOrCreateAddressAsync(
            customerId,
            model.Id,
            cancellationToken);

        if (model.IsDefault)
        {
            await ClearOtherDefaultAddressesAsync(
                customerId,
                address.Id,
                cancellationToken);
        }

        MapAddress(model, address);
        await context.SaveChangesAsync(cancellationToken);
        return address.Id;
    }

    public async Task ArchiveAddressAsync(
        Guid customerId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var address = await context.Addresses.FirstOrDefaultAsync(
            candidate => candidate.Id == id && candidate.CustomerId == customerId,
            cancellationToken)
            ?? throw new InvalidOperationException("Address not found.");

        address.IsArchived = true;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task MergeCartAsync(
        Guid customerId,
        IReadOnlyList<CustomerCartLine> anonymousItems,
        CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(customerId, cancellationToken);

        foreach (var item in anonymousItems)
        {
            MergeCartItem(cart, item);
        }

        cart.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerCartLine>> GetCartAsync(
        Guid customerId,
        CancellationToken cancellationToken = default) =>
        await context.CustomerCartItems
            .AsNoTracking()
            .Where(item =>
                item.CustomerCart.CustomerId == customerId &&
                item.ProductVariant.IsActive &&
                item.ProductVariant.Product.IsActive &&
                !item.ProductVariant.Product.IsArchived)
            .Select(item => new CustomerCartLine(
                item.ProductVariant.ProductId,
                item.ProductVariantId,
                item.ProductVariant.Product.Slug,
                item.ProductVariant.Product.Name,
                item.ProductVariant.Product.Images
                    .OrderByDescending(image => image.IsPrimary)
                    .ThenBy(image => image.DisplayOrder)
                    .Select(image => image.Url)
                    .FirstOrDefault() ?? item.ProductVariant.Product.ImageUrl,
                item.ProductVariant.Color + " / " + item.ProductVariant.Size,
                item.ProductVariant.Product.Price,
                item.Quantity))
            .ToListAsync(cancellationToken);

    public async Task SaveCartAsync(
        Guid customerId,
        IReadOnlyList<CustomerCartLine> items,
        CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(customerId, cancellationToken);
        context.CustomerCartItems.RemoveRange(cart.Items);

        cart.Items = items
            .GroupBy(item => item.VariantId)
            .Select(group => new CustomerCartItem
            {
                Id = Guid.NewGuid(),
                ProductVariantId = group.Key,
                Quantity = Math.Clamp(group.Sum(item => item.Quantity), 1, 10)
            })
            .ToList();
        cart.UpdatedAtUtc = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WishlistProduct>> GetWishlistAsync(
        Guid customerId,
        CancellationToken cancellationToken = default) =>
        await context.WishlistItems
            .AsNoTracking()
            .Where(item =>
                item.CustomerId == customerId &&
                item.Product.IsActive &&
                !item.Product.IsArchived)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new WishlistProduct(
                item.ProductId,
                item.Product.Name,
                item.Product.Slug,
                item.Product.Price,
                item.Product.Images
                    .OrderByDescending(image => image.IsPrimary)
                    .ThenBy(image => image.DisplayOrder)
                    .Select(image => image.Url)
                    .FirstOrDefault() ?? item.Product.ImageUrl))
            .ToListAsync(cancellationToken);

    public async Task<bool> ToggleWishlistAsync(
        Guid customerId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var existing = await context.WishlistItems.FirstOrDefaultAsync(
            item => item.CustomerId == customerId && item.ProductId == productId,
            cancellationToken);

        if (existing is null)
        {
            context.WishlistItems.Add(new WishlistItem
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                ProductId = productId
            });
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }

        context.WishlistItems.Remove(existing);
        await context.SaveChangesAsync(cancellationToken);
        return false;
    }

    public async Task<IReadOnlyList<CustomerOrderSummary>> GetOrdersAsync(
        Guid customerId,
        CancellationToken cancellationToken = default) =>
        await context.Orders
            .AsNoTracking()
            .Where(order => order.CustomerId == customerId)
            .OrderByDescending(order => order.CreatedAtUtc)
            .Select(order => new CustomerOrderSummary(
                order.Id,
                order.Number,
                order.Status.ToString(),
                order.Total,
                order.Currency,
                order.CreatedAtUtc,
                order.Items.Sum(item => item.Quantity)))
            .ToListAsync(cancellationToken);

    private async Task<Address> FindOrCreateAddressAsync(
        Guid customerId,
        Guid? addressId,
        CancellationToken cancellationToken)
    {
        if (addressId is null)
        {
            var address = new Address
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId
            };
            context.Addresses.Add(address);
            return address;
        }

        return await context.Addresses.FirstOrDefaultAsync(
            candidate =>
                candidate.Id == addressId.Value &&
                candidate.CustomerId == customerId,
            cancellationToken)
            ?? throw new InvalidOperationException("Address not found.");
    }

    private Task ClearOtherDefaultAddressesAsync(
        Guid customerId,
        Guid addressId,
        CancellationToken cancellationToken) =>
        context.Addresses
            .Where(address =>
                address.CustomerId == customerId && address.Id != addressId)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(address => address.IsDefault, false),
                cancellationToken);

    private async Task<CustomerCart> GetOrCreateCartAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var cart = await context.CustomerCarts
            .Include(candidate => candidate.Items)
            .FirstOrDefaultAsync(
                candidate => candidate.CustomerId == customerId,
                cancellationToken);

        if (cart is not null)
        {
            return cart;
        }

        cart = new CustomerCart
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId
        };
        context.CustomerCarts.Add(cart);
        return cart;
    }

    private static void MergeCartItem(CustomerCart cart, CustomerCartLine item)
    {
        var existing = cart.Items.FirstOrDefault(
            candidate => candidate.ProductVariantId == item.VariantId);

        if (existing is null)
        {
            cart.Items.Add(new CustomerCartItem
            {
                Id = Guid.NewGuid(),
                ProductVariantId = item.VariantId,
                Quantity = Math.Clamp(item.Quantity, 1, 10)
            });
            return;
        }

        existing.Quantity = Math.Clamp(existing.Quantity + item.Quantity, 1, 10);
    }

    private static void MapAddress(CustomerAddress source, Address target)
    {
        target.Label = source.Label.Trim();
        target.RecipientName = source.RecipientName.Trim();
        target.PhoneNumber = source.PhoneNumber.Trim();
        target.Line1 = source.Line1.Trim();
        target.Line2 = source.Line2.Trim();
        target.City = source.City.Trim();
        target.StateOrProvince = source.StateOrProvince.Trim();
        target.PostalCode = source.PostalCode.Trim();
        target.CountryCode = source.CountryCode.Trim().ToUpperInvariant();
        target.IsDefault = source.IsDefault;
    }

    private static Expression<Func<Address, CustomerAddress>> AddressProjection =>
        address => new CustomerAddress
        {
            Id = address.Id,
            Label = address.Label,
            RecipientName = address.RecipientName,
            PhoneNumber = address.PhoneNumber,
            Line1 = address.Line1,
            Line2 = address.Line2,
            City = address.City,
            StateOrProvince = address.StateOrProvince,
            PostalCode = address.PostalCode,
            CountryCode = address.CountryCode,
            IsDefault = address.IsDefault
        };
}
