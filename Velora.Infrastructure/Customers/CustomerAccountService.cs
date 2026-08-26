using Microsoft.EntityFrameworkCore;
using Velora.Application.Customers;
using Velora.Domain.Customers;
using Velora.Infrastructure.Persistence;

namespace Velora.Infrastructure.Customers;

internal sealed class CustomerAccountService(ApplicationDbContext context) : ICustomerAccountService
{
    public Task<CustomerProfile?> GetProfileAsync(Guid customerId, CancellationToken cancellationToken = default) => context.Users.AsNoTracking().Where(x => x.Id == customerId).Select(x => new CustomerProfile { FirstName = x.FirstName, LastName = x.LastName, Email = x.Email ?? string.Empty, PhoneNumber = x.PhoneNumber ?? string.Empty }).FirstOrDefaultAsync(cancellationToken);
    public async Task UpdateProfileAsync(Guid customerId, CustomerProfile model, CancellationToken cancellationToken = default) { var user = await context.Users.FindAsync([customerId], cancellationToken) ?? throw new InvalidOperationException("User not found."); user.FirstName = model.FirstName.Trim(); user.LastName = model.LastName.Trim(); user.PhoneNumber = model.PhoneNumber.Trim(); await context.SaveChangesAsync(cancellationToken); }
    public async Task<IReadOnlyList<CustomerAddress>> GetAddressesAsync(Guid customerId, CancellationToken cancellationToken = default) => await context.Addresses.AsNoTracking().Where(x => x.CustomerId == customerId && !x.IsArchived).OrderByDescending(x => x.IsDefault).Select(MapAddressExpression).ToListAsync(cancellationToken);
    public Task<CustomerAddress?> GetAddressAsync(Guid customerId, Guid id, CancellationToken cancellationToken = default) => context.Addresses.AsNoTracking().Where(x => x.CustomerId == customerId && x.Id == id && !x.IsArchived).Select(MapAddressExpression).FirstOrDefaultAsync(cancellationToken);
    public async Task<Guid> SaveAddressAsync(Guid customerId, CustomerAddress model, CancellationToken cancellationToken = default)
    {
        var address = model.Id is null ? new Address { Id = Guid.NewGuid(), CustomerId = customerId } : await context.Addresses.FirstOrDefaultAsync(x => x.Id == model.Id && x.CustomerId == customerId, cancellationToken) ?? throw new InvalidOperationException("Address not found.");
        if (model.Id is null) context.Addresses.Add(address);
        if (model.IsDefault) await context.Addresses.Where(x => x.CustomerId == customerId && x.Id != address.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, false), cancellationToken);
        address.Label = model.Label.Trim(); address.RecipientName = model.RecipientName.Trim(); address.PhoneNumber = model.PhoneNumber.Trim(); address.Line1 = model.Line1.Trim(); address.Line2 = model.Line2.Trim(); address.City = model.City.Trim(); address.StateOrProvince = model.StateOrProvince.Trim(); address.PostalCode = model.PostalCode.Trim(); address.CountryCode = model.CountryCode.Trim().ToUpperInvariant(); address.IsDefault = model.IsDefault;
        await context.SaveChangesAsync(cancellationToken); return address.Id;
    }
    public async Task ArchiveAddressAsync(Guid customerId, Guid id, CancellationToken cancellationToken = default) { var address = await context.Addresses.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId, cancellationToken) ?? throw new InvalidOperationException("Address not found."); address.IsArchived = true; await context.SaveChangesAsync(cancellationToken); }
    public async Task MergeCartAsync(Guid customerId, IReadOnlyList<CustomerCartLine> anonymousItems, CancellationToken cancellationToken = default)
    {
        var cart = await context.CustomerCarts.Include(x => x.Items).FirstOrDefaultAsync(x => x.CustomerId == customerId, cancellationToken);
        if (cart is null) { cart = new CustomerCart { Id = Guid.NewGuid(), CustomerId = customerId }; context.CustomerCarts.Add(cart); }
        foreach (var item in anonymousItems) { var existing = cart.Items.FirstOrDefault(x => x.ProductVariantId == item.VariantId); if (existing is null) cart.Items.Add(new CustomerCartItem { Id = Guid.NewGuid(), ProductVariantId = item.VariantId, Quantity = Math.Clamp(item.Quantity, 1, 10) }); else existing.Quantity = Math.Min(10, existing.Quantity + item.Quantity); }
        cart.UpdatedAtUtc = DateTime.UtcNow; await context.SaveChangesAsync(cancellationToken);
    }
    public async Task<IReadOnlyList<CustomerCartLine>> GetCartAsync(Guid customerId, CancellationToken cancellationToken = default) => await context.CustomerCartItems.AsNoTracking().Where(x => x.CustomerCart.CustomerId == customerId && x.ProductVariant.IsActive && x.ProductVariant.Product.IsActive && !x.ProductVariant.Product.IsArchived).Select(x => new CustomerCartLine(x.ProductVariant.ProductId, x.ProductVariantId, x.ProductVariant.Product.Slug, x.ProductVariant.Product.Name, x.ProductVariant.Product.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault() ?? x.ProductVariant.Product.ImageUrl, x.ProductVariant.Color + " / " + x.ProductVariant.Size, x.ProductVariant.Product.Price, x.Quantity)).ToListAsync(cancellationToken);
    public async Task SaveCartAsync(Guid customerId, IReadOnlyList<CustomerCartLine> items, CancellationToken cancellationToken = default)
    {
        var cart = await context.CustomerCarts.Include(x => x.Items).FirstOrDefaultAsync(x => x.CustomerId == customerId, cancellationToken);
        if (cart is null) { cart = new CustomerCart { Id = Guid.NewGuid(), CustomerId = customerId }; context.CustomerCarts.Add(cart); }
        context.CustomerCartItems.RemoveRange(cart.Items); cart.Items = items.Select(x => new CustomerCartItem { Id = Guid.NewGuid(), ProductVariantId = x.VariantId, Quantity = x.Quantity }).ToList(); cart.UpdatedAtUtc = DateTime.UtcNow; await context.SaveChangesAsync(cancellationToken);
    }
    public async Task<IReadOnlyList<WishlistProduct>> GetWishlistAsync(Guid customerId, CancellationToken cancellationToken = default) => await context.WishlistItems.AsNoTracking().Where(x => x.CustomerId == customerId && x.Product.IsActive && !x.Product.IsArchived).OrderByDescending(x => x.CreatedAtUtc).Select(x => new WishlistProduct(x.ProductId, x.Product.Name, x.Product.Slug, x.Product.Price, x.Product.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault() ?? x.Product.ImageUrl)).ToListAsync(cancellationToken);
    public async Task<bool> ToggleWishlistAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default) { var existing = await context.WishlistItems.FirstOrDefaultAsync(x => x.CustomerId == customerId && x.ProductId == productId, cancellationToken); if (existing is null) { context.WishlistItems.Add(new WishlistItem { Id = Guid.NewGuid(), CustomerId = customerId, ProductId = productId }); await context.SaveChangesAsync(cancellationToken); return true; } context.WishlistItems.Remove(existing); await context.SaveChangesAsync(cancellationToken); return false; }
    public async Task<IReadOnlyList<CustomerOrderSummary>> GetOrdersAsync(Guid customerId, CancellationToken cancellationToken = default) => await context.Orders.AsNoTracking().Where(x => x.CustomerId == customerId).OrderByDescending(x => x.CreatedAtUtc).Select(x => new CustomerOrderSummary(x.Id, x.Number, x.Status.ToString(), x.Total, x.Currency, x.CreatedAtUtc, x.Items.Sum(i => i.Quantity))).ToListAsync(cancellationToken);
    private static System.Linq.Expressions.Expression<Func<Address, CustomerAddress>> MapAddressExpression => x => new CustomerAddress { Id = x.Id, Label = x.Label, RecipientName = x.RecipientName, PhoneNumber = x.PhoneNumber, Line1 = x.Line1, Line2 = x.Line2, City = x.City, StateOrProvince = x.StateOrProvince, PostalCode = x.PostalCode, CountryCode = x.CountryCode, IsDefault = x.IsDefault };
}
