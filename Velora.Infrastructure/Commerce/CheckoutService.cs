using System.Data;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Velora.Application.Commerce;
using Velora.Application.Communication;
using Velora.Domain.Commerce;
using Velora.Infrastructure.Persistence;
using Velora.Infrastructure.Observability;

namespace Velora.Infrastructure.Commerce;

internal sealed class CheckoutService(ApplicationDbContext context, ITransactionalEmailSender email, ILogger<CheckoutService> logger, OrderMetrics metrics) : ICheckoutService
{
    private static readonly ActivitySource ActivitySource = new("Velora.Orders");

    public async Task<CheckoutQuote> QuoteAsync(IReadOnlyList<CheckoutLine> items, string? couponCode, CancellationToken cancellationToken = default)
    {
        var variantIds = items.Select(x => x.VariantId).Distinct().ToList();
        var prices = await context.ProductVariants.AsNoTracking().Where(x => variantIds.Contains(x.Id) && x.IsActive && x.Product.IsActive && !x.Product.IsArchived).Select(x => new { x.Id, x.Product.Price }).ToDictionaryAsync(x => x.Id, cancellationToken);
        if (prices.Count != variantIds.Count) throw new InvalidOperationException("One or more cart items are no longer available.");
        var subtotal = items.Sum(x => prices[x.VariantId].Price * x.Quantity);
        var discount = await CalculateDiscountAsync(subtotal, couponCode, cancellationToken);
        var delivery = subtotal - discount >= 200m ? 0m : 15m;
        return new CheckoutQuote(subtotal, discount, delivery, subtotal - discount + delivery, "USD");
    }

    public async Task<OrderConfirmation> PlaceCashOnDeliveryOrderAsync(CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("PlaceCashOnDeliveryOrder");
        activity?.SetTag("order.item_count", request.Items.Count);
        if (request.Items.Count == 0) throw new InvalidOperationException("Your bag is empty.");
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var variantIds = request.Items.Select(x => x.VariantId).Distinct().ToList();
            var variants = await context.ProductVariants.Include(x => x.Product).ThenInclude(x => x.Images).Where(x => variantIds.Contains(x.Id) && x.IsActive && x.Product.IsActive && !x.Product.IsArchived).ToDictionaryAsync(x => x.Id, cancellationToken);
            if (variants.Count != variantIds.Count) throw new InvalidOperationException("One or more items are unavailable.");
            foreach (var line in request.Items) if (line.Quantity <= 0 || variants[line.VariantId].StockQuantity < line.Quantity) throw new InvalidOperationException($"Insufficient stock for {variants[line.VariantId].Product.Name}.");

            var subtotal = request.Items.Sum(x => variants[x.VariantId].Product.Price * x.Quantity);
            var discount = await CalculateDiscountAsync(subtotal, request.CouponCode, cancellationToken);
            var delivery = subtotal - discount >= 200m ? 0m : 15m;
            var order = new Order
            {
                Id = Guid.NewGuid(), Number = $"VEL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant(), CustomerId = request.CustomerId, Status = OrderStatus.Confirmed,
                Subtotal = subtotal, DiscountTotal = discount, DeliveryTotal = delivery, Total = subtotal - discount + delivery, CouponCode = request.CouponCode?.Trim().ToUpperInvariant(),
                CustomerEmail = request.CustomerEmail.Trim(), RecipientName = request.RecipientName.Trim(), PhoneNumber = request.PhoneNumber.Trim(), AddressLine1 = request.AddressLine1.Trim(), AddressLine2 = request.AddressLine2.Trim(), City = request.City.Trim(), StateOrProvince = request.StateOrProvince.Trim(), PostalCode = request.PostalCode.Trim(), CountryCode = request.CountryCode.Trim().ToUpperInvariant(), CustomerNote = request.CustomerNote.Trim()
            };
            foreach (var line in request.Items)
            {
                var variant = variants[line.VariantId]; var product = variant.Product; variant.StockQuantity -= line.Quantity;
                order.Items.Add(new OrderItem { Id = Guid.NewGuid(), ProductId = product.Id, ProductVariantId = variant.Id, ProductName = product.Name, ProductSlug = product.Slug, Sku = variant.Sku, Option = $"{variant.Color} / {variant.Size}", ImageUrl = product.Images.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.DisplayOrder).Select(x => x.Url).FirstOrDefault() ?? product.ImageUrl, UnitPrice = product.Price, Quantity = line.Quantity, LineTotal = product.Price * line.Quantity });
            }
            order.Payments.Add(new Payment { Id = Guid.NewGuid(), Method = "CashOnDelivery", Status = PaymentStatus.DueOnDelivery, Amount = order.Total });
            order.Shipments.Add(new Shipment { Id = Guid.NewGuid(), Status = ShipmentStatus.Pending });
            order.StatusHistory.Add(new OrderStatusHistory { Id = Guid.NewGuid(), Status = OrderStatus.Confirmed, Note = "Cash-on-delivery order placed." });
            context.Orders.Add(order);
            var coupon = string.IsNullOrWhiteSpace(request.CouponCode) ? null : await context.DiscountCoupons.FirstOrDefaultAsync(x => x.Code == request.CouponCode.Trim().ToUpper(), cancellationToken); if (coupon is not null) coupon.UsageCount++;
            var cart = await context.CustomerCarts.Include(x => x.Items).FirstOrDefaultAsync(x => x.CustomerId == request.CustomerId, cancellationToken); if (cart is not null) context.CustomerCartItems.RemoveRange(cart.Items);
            await context.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
            activity?.SetTag("order.status", "confirmed"); activity?.SetStatus(ActivityStatusCode.Ok);
            try
            {
                await email.SendAsync(order.CustomerEmail, $"Your Velora order {order.Number}", $"<h1>Thank you for your order</h1><p>Your order <strong>{order.Number}</strong> has been confirmed. Total due on delivery: <strong>{order.Total:N2} {order.Currency}</strong>.</p>", cancellationToken);
            }
            catch (Exception emailException)
            {
                logger.LogWarning(emailException, "Order {OrderNumber} was placed but its confirmation email could not be sent", order.Number);
            }
            metrics.RecordPlaced((double)order.Total);
            return new OrderConfirmation(order.Id, order.Number, order.Total, order.Currency);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(cancellationToken); activity?.SetStatus(ActivityStatusCode.Error, exception.Message); logger.LogError(exception, "Cash-on-delivery checkout failed"); throw;
        }
    }

    public Task<OrderDetailsModel?> GetOrderAsync(Guid orderId, Guid customerId, CancellationToken cancellationToken = default) => context.Orders.AsNoTracking().Where(x => x.Id == orderId && x.CustomerId == customerId).Select(x => new OrderDetailsModel(x.Id, x.Number, x.Status.ToString(), x.Subtotal, x.DiscountTotal, x.DeliveryTotal, x.Total, x.Currency, x.CreatedAtUtc, x.Items.Select(i => new OrderLineModel(i.ProductName, i.Option, i.Sku, i.ImageUrl, i.UnitPrice, i.Quantity, i.LineTotal)).ToList(), x.AddressLine1 + ", " + x.City + ", " + x.CountryCode, x.Payments.OrderByDescending(p => p.CreatedAtUtc).Select(p => p.Status.ToString()).FirstOrDefault() ?? "Pending")).FirstOrDefaultAsync(cancellationToken);

    private async Task<decimal> CalculateDiscountAsync(decimal subtotal, string? code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code)) return 0m; var now = DateTime.UtcNow; var normalized = code.Trim().ToUpperInvariant();
        var coupon = await context.DiscountCoupons.AsNoTracking().FirstOrDefaultAsync(x => x.Code == normalized && x.IsActive && !x.IsArchived && x.StartsAtUtc <= now && (x.EndsAtUtc == null || x.EndsAtUtc >= now) && (x.UsageLimit == null || x.UsageCount < x.UsageLimit), cancellationToken);
        if (coupon is null || subtotal < (coupon.MinimumOrderAmount ?? 0m)) return 0m;
        return coupon.Type == DiscountType.Percentage ? Math.Min(subtotal, subtotal * coupon.Value / 100m) : Math.Min(subtotal, coupon.Value);
    }
}
