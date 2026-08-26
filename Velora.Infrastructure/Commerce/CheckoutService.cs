using System.Data;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Velora.Application.Commerce;
using Velora.Application.Communication;
using Velora.Domain.Catalog;
using Velora.Domain.Commerce;
using Velora.Infrastructure.Observability;
using Velora.Infrastructure.Persistence;

namespace Velora.Infrastructure.Commerce;

internal sealed class CheckoutService(
    ApplicationDbContext context,
    ITransactionalEmailSender email,
    ILogger<CheckoutService> logger,
    OrderMetrics metrics) : ICheckoutService
{
    private const decimal FreeDeliveryThreshold = 200m;
    private const decimal StandardDeliveryFee = 15m;
    private static readonly ActivitySource ActivitySource = new(OrderMetrics.OrderActivitySource);

    public async Task<CheckoutQuote> QuoteAsync(
        IReadOnlyList<CheckoutLine> items,
        string? couponCode,
        CancellationToken cancellationToken = default)
    {
        var lines = NormalizeLines(items);
        var variants = await LoadAvailableVariantsAsync(
            lines.Select(line => line.VariantId),
            includeImages: false,
            cancellationToken);

        ValidateAvailability(lines, variants);
        var subtotal = CalculateSubtotal(lines, variants);
        var coupon = await FindEligibleCouponAsync(subtotal, couponCode, cancellationToken);
        var discount = CalculateDiscount(subtotal, coupon);
        var delivery = CalculateDelivery(subtotal, discount);

        return new CheckoutQuote(
            subtotal,
            discount,
            delivery,
            subtotal - discount + delivery,
            Currency: "USD");
    }

    public async Task<OrderConfirmation> PlaceCashOnDeliveryOrderAsync(
        CheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("PlaceCashOnDeliveryOrder");
        var lines = NormalizeLines(request.Items);
        activity?.SetTag("order.item_count", lines.Count);

        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var variants = await LoadAvailableVariantsAsync(
                lines.Select(line => line.VariantId),
                includeImages: true,
                cancellationToken);

            ValidateAvailability(lines, variants);
            var subtotal = CalculateSubtotal(lines, variants);
            var coupon = await FindEligibleCouponAsync(
                subtotal,
                request.CouponCode,
                cancellationToken,
                track: true);
            var discount = CalculateDiscount(subtotal, coupon);
            var delivery = CalculateDelivery(subtotal, discount);
            var order = CreateOrder(request, subtotal, discount, delivery, coupon?.Code);

            AddOrderItemsAndReduceStock(order, lines, variants);
            AddFulfillmentRecords(order);

            if (coupon is not null && discount > 0)
            {
                coupon.UsageCount++;
            }

            context.Orders.Add(order);
            await ClearCustomerCartAsync(request.CustomerId, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            activity?.SetTag("order.status", "confirmed");
            activity?.SetStatus(ActivityStatusCode.Ok);
            await SendConfirmationEmailAsync(order, cancellationToken);
            metrics.RecordPlaced((double)order.Total);

            return new OrderConfirmation(order.Id, order.Number, order.Total, order.Currency);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            logger.LogError(exception, "Cash-on-delivery checkout failed");
            throw;
        }
    }

    public Task<OrderDetailsModel?> GetOrderAsync(
        Guid orderId,
        Guid customerId,
        CancellationToken cancellationToken = default) =>
        context.Orders
            .AsNoTracking()
            .Where(order => order.Id == orderId && order.CustomerId == customerId)
            .Select(order => new OrderDetailsModel(
                order.Id,
                order.Number,
                order.Status.ToString(),
                order.Subtotal,
                order.DiscountTotal,
                order.DeliveryTotal,
                order.Total,
                order.Currency,
                order.CreatedAtUtc,
                order.Items.Select(item => new OrderLineModel(
                    item.ProductName,
                    item.Option,
                    item.Sku,
                    item.ImageUrl,
                    item.UnitPrice,
                    item.Quantity,
                    item.LineTotal)).ToList(),
                order.AddressLine1 + ", " + order.City + ", " + order.CountryCode,
                order.Payments
                    .OrderByDescending(payment => payment.CreatedAtUtc)
                    .Select(payment => payment.Status.ToString())
                    .FirstOrDefault() ?? "Pending"))
            .FirstOrDefaultAsync(cancellationToken);

    private static IReadOnlyList<CheckoutLine> NormalizeLines(IReadOnlyList<CheckoutLine> items)
    {
        if (items.Count == 0)
        {
            throw new InvalidOperationException("Your bag is empty.");
        }

        if (items.Any(line => line.Quantity <= 0))
        {
            throw new InvalidOperationException("Item quantities must be greater than zero.");
        }

        return items
            .GroupBy(line => line.VariantId)
            .Select(group => new CheckoutLine(
                group.First().ProductId,
                group.Key,
                group.Sum(line => line.Quantity)))
            .ToList();
    }

    private async Task<Dictionary<Guid, ProductVariant>> LoadAvailableVariantsAsync(
        IEnumerable<Guid> variantIds,
        bool includeImages,
        CancellationToken cancellationToken)
    {
        var ids = variantIds.Distinct().ToList();
        IQueryable<ProductVariant> query = context.ProductVariants;

        if (includeImages)
        {
            query = query
                .Include(variant => variant.Product)
                .ThenInclude(product => product.Images);
        }

        return await query
            .Where(variant =>
                ids.Contains(variant.Id) &&
                variant.IsActive &&
                variant.Product.IsActive &&
                !variant.Product.IsArchived)
            .ToDictionaryAsync(variant => variant.Id, cancellationToken);
    }

    private static void ValidateAvailability(
        IReadOnlyList<CheckoutLine> lines,
        IReadOnlyDictionary<Guid, ProductVariant> variants)
    {
        if (variants.Count != lines.Count)
        {
            throw new InvalidOperationException("One or more cart items are no longer available.");
        }

        foreach (var line in lines)
        {
            var variant = variants[line.VariantId];
            if (variant.StockQuantity < line.Quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock for {variant.Product.Name}.");
            }
        }
    }

    private static decimal CalculateSubtotal(
        IEnumerable<CheckoutLine> lines,
        IReadOnlyDictionary<Guid, ProductVariant> variants) =>
        lines.Sum(line => variants[line.VariantId].Product.Price * line.Quantity);

    private async Task<DiscountCoupon?> FindEligibleCouponAsync(
        decimal subtotal,
        string? code,
        CancellationToken cancellationToken,
        bool track = false)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var normalizedCode = code.Trim().ToUpperInvariant();
        var query = context.DiscountCoupons.Where(coupon =>
            coupon.Code == normalizedCode &&
            coupon.IsActive &&
            !coupon.IsArchived &&
            coupon.StartsAtUtc <= now &&
            (coupon.EndsAtUtc == null || coupon.EndsAtUtc >= now) &&
            (coupon.UsageLimit == null || coupon.UsageCount < coupon.UsageLimit));

        if (!track)
        {
            query = query.AsNoTracking();
        }

        var coupon = await query.FirstOrDefaultAsync(cancellationToken);
        return coupon is not null && subtotal >= (coupon.MinimumOrderAmount ?? 0m)
            ? coupon
            : null;
    }

    private static decimal CalculateDiscount(decimal subtotal, DiscountCoupon? coupon)
    {
        if (coupon is null)
        {
            return 0m;
        }

        var discount = coupon.Type == DiscountType.Percentage
            ? subtotal * coupon.Value / 100m
            : coupon.Value;

        return Math.Min(subtotal, discount);
    }

    private static decimal CalculateDelivery(decimal subtotal, decimal discount) =>
        subtotal - discount >= FreeDeliveryThreshold ? 0m : StandardDeliveryFee;

    private static Order CreateOrder(
        CheckoutRequest request,
        decimal subtotal,
        decimal discount,
        decimal delivery,
        string? couponCode) =>
        new()
        {
            Id = Guid.NewGuid(),
            Number = CreateOrderNumber(),
            CustomerId = request.CustomerId,
            Status = OrderStatus.Confirmed,
            Subtotal = subtotal,
            DiscountTotal = discount,
            DeliveryTotal = delivery,
            Total = subtotal - discount + delivery,
            CouponCode = couponCode,
            CustomerEmail = request.CustomerEmail.Trim(),
            RecipientName = request.RecipientName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            AddressLine1 = request.AddressLine1.Trim(),
            AddressLine2 = request.AddressLine2.Trim(),
            City = request.City.Trim(),
            StateOrProvince = request.StateOrProvince.Trim(),
            PostalCode = request.PostalCode.Trim(),
            CountryCode = request.CountryCode.Trim().ToUpperInvariant(),
            CustomerNote = request.CustomerNote.Trim()
        };

    private static string CreateOrderNumber() =>
        $"VEL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..22].ToUpperInvariant();

    private static void AddOrderItemsAndReduceStock(
        Order order,
        IEnumerable<CheckoutLine> lines,
        IReadOnlyDictionary<Guid, ProductVariant> variants)
    {
        foreach (var line in lines)
        {
            var variant = variants[line.VariantId];
            var product = variant.Product;
            variant.StockQuantity -= line.Quantity;

            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ProductVariantId = variant.Id,
                ProductName = product.Name,
                ProductSlug = product.Slug,
                Sku = variant.Sku,
                Option = $"{variant.Color} / {variant.Size}",
                ImageUrl = SelectPrimaryImage(product),
                UnitPrice = product.Price,
                Quantity = line.Quantity,
                LineTotal = product.Price * line.Quantity
            });
        }
    }

    private static string SelectPrimaryImage(Product product) =>
        product.Images
            .OrderByDescending(image => image.IsPrimary)
            .ThenBy(image => image.DisplayOrder)
            .Select(image => image.Url)
            .FirstOrDefault() ?? product.ImageUrl;

    private static void AddFulfillmentRecords(Order order)
    {
        order.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            Method = "CashOnDelivery",
            Status = PaymentStatus.DueOnDelivery,
            Amount = order.Total
        });
        order.Shipments.Add(new Shipment
        {
            Id = Guid.NewGuid(),
            Status = ShipmentStatus.Pending
        });
        order.StatusHistory.Add(new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            Status = OrderStatus.Confirmed,
            Note = "Cash-on-delivery order placed."
        });
    }

    private async Task ClearCustomerCartAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var cart = await context.CustomerCarts
            .Include(customerCart => customerCart.Items)
            .FirstOrDefaultAsync(
                customerCart => customerCart.CustomerId == customerId,
                cancellationToken);

        if (cart is not null)
        {
            context.CustomerCartItems.RemoveRange(cart.Items);
        }
    }

    private async Task SendConfirmationEmailAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        try
        {
            var subject = $"Your Velora order {order.Number}";
            var body = $"""
                <h1>Thank you for your order</h1>
                <p>Your order <strong>{order.Number}</strong> has been confirmed.</p>
                <p>Total due on delivery: <strong>{order.Total:N2} {order.Currency}</strong>.</p>
                """;
            await email.SendAsync(order.CustomerEmail, subject, body, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Order {OrderNumber} was placed but its confirmation email could not be sent",
                order.Number);
        }
    }
}
