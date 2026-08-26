using System.Text.Json;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Velora.Application.Administration;
using Velora.Domain.Administration;
using Velora.Domain.Commerce;
using Velora.Infrastructure.Persistence;

namespace Velora.Infrastructure.Commerce;

internal sealed class AdminCommerceService(ApplicationDbContext context) : IAdminCommerceService
{
    private const int MaximumPageSize = 100;

    public async Task<AdminDashboardStats> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        var recentOrders = await GetOrdersAsync(null, 1, 8, cancellationToken);
        var closedStatuses = new[] { OrderStatus.Delivered, OrderStatus.Cancelled };
        var activeProducts = await context.Products.CountAsync(
            product => product.IsActive && !product.IsArchived,
            cancellationToken);
        var lowStockVariants = await context.ProductVariants.CountAsync(
            variant => variant.IsActive &&
                       variant.StockQuantity <= variant.LowStockThreshold,
            cancellationToken);
        var activeCustomers = await context.Users.CountAsync(
            user => user.IsActive,
            cancellationToken);
        var openOrders = await context.Orders.CountAsync(
            order => !closedStatuses.Contains(order.Status),
            cancellationToken);
        var revenue = await context.Orders
            .Where(order => order.Status != OrderStatus.Cancelled)
            .SumAsync(order => (decimal?)order.Total, cancellationToken) ?? 0m;

        return new AdminDashboardStats(
            activeProducts,
            lowStockVariants,
            activeCustomers,
            openOrders,
            revenue,
            recentOrders);
    }

    public async Task<IReadOnlyList<AdminOrderListItem>> GetOrdersAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Orders.AsNoTracking();
        if (Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(order => order.Status == parsedStatus);
        }

        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, MaximumPageSize);

        return await query
            .OrderByDescending(order => order.CreatedAtUtc)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(order => new AdminOrderListItem(
                order.Id,
                order.Number,
                order.CustomerEmail,
                order.Status.ToString(),
                order.Total,
                order.Currency,
                order.Items.Sum(item => item.Quantity),
                order.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public Task<AdminOrderDetails?> GetOrderAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        context.Orders
            .AsNoTracking()
            .Where(order => order.Id == id)
            .Select(order => new AdminOrderDetails(
                order.Id,
                order.Number,
                order.CustomerEmail,
                order.RecipientName,
                order.PhoneNumber,
                order.AddressLine1 + ", " + order.City + ", " + order.CountryCode,
                order.Status.ToString(),
                order.Payments
                    .OrderByDescending(payment => payment.CreatedAtUtc)
                    .Select(payment => payment.Status.ToString())
                    .FirstOrDefault() ?? "Pending",
                order.Shipments
                    .Select(shipment => shipment.Status.ToString())
                    .FirstOrDefault() ?? "Pending",
                order.Total,
                order.Currency,
                order.CreatedAtUtc,
                order.Items
                    .Select(item => new AdminOrderLine(
                        item.ProductName,
                        item.Sku,
                        item.Option,
                        item.Quantity,
                        item.LineTotal))
                    .ToList(),
                order.StatusHistory
                    .OrderByDescending(history => history.CreatedAtUtc)
                    .Select(history =>
                        history.CreatedAtUtc.ToString("u") +
                        " — " + history.Status + ": " + history.Note)
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task UpdateOrderAsync(
        Guid id,
        string status,
        string shipmentStatus,
        string note,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var order = await context.Orders
            .Include(candidate => candidate.Shipments)
            .Include(candidate => candidate.Payments)
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Order not found.");

        if (!Enum.TryParse<OrderStatus>(status, true, out var nextOrderStatus))
        {
            throw new InvalidOperationException("Invalid order status.");
        }

        if (!Enum.TryParse<ShipmentStatus>(
                shipmentStatus,
                true,
                out var nextShipmentStatus))
        {
            throw new InvalidOperationException("Invalid shipment status.");
        }

        order.Status = nextOrderStatus;
        order.UpdatedAtUtc = DateTime.UtcNow;
        UpdateShipment(order, nextShipmentStatus);
        UpdateCashPayment(order, nextOrderStatus);

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            Status = nextOrderStatus,
            Note = note.Trim(),
            ChangedByUserId = actorId
        });

        AddAudit(
            "Order.StatusChanged",
            nameof(Order),
            id.ToString(),
            actorId,
            ipAddress,
            new { status, shipmentStatus });
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminCouponModel>> GetCouponsAsync(
        CancellationToken cancellationToken = default) =>
        await context.DiscountCoupons
            .AsNoTracking()
            .Where(coupon => !coupon.IsArchived)
            .OrderByDescending(coupon => coupon.StartsAtUtc)
            .Select(CouponProjection)
            .ToListAsync(cancellationToken);

    public Task<AdminCouponModel?> GetCouponAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        context.DiscountCoupons
            .AsNoTracking()
            .Where(coupon => coupon.Id == id)
            .Select(CouponProjection)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Guid> SaveCouponAsync(
        AdminCouponModel model,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<DiscountType>(model.Type, true, out var discountType))
        {
            throw new InvalidOperationException("Invalid discount type.");
        }

        var coupon = model.Id is null
            ? new DiscountCoupon { Id = Guid.NewGuid() }
            : await context.DiscountCoupons.FindAsync([model.Id.Value], cancellationToken)
              ?? throw new InvalidOperationException("Coupon not found.");

        if (model.Id is null)
        {
            context.DiscountCoupons.Add(coupon);
        }

        coupon.Code = model.Code.Trim().ToUpperInvariant();
        coupon.Type = discountType;
        coupon.Value = model.Value;
        coupon.MinimumOrderAmount = model.MinimumOrderAmount;
        coupon.UsageLimit = model.UsageLimit;
        coupon.StartsAtUtc = model.StartsAtUtc;
        coupon.EndsAtUtc = model.EndsAtUtc;
        coupon.IsActive = model.IsActive;

        AddAudit(
            model.Id is null ? "Coupon.Created" : "Coupon.Updated",
            nameof(DiscountCoupon),
            coupon.Id.ToString(),
            actorId,
            ipAddress,
            new { coupon.Code });
        await context.SaveChangesAsync(cancellationToken);
        return coupon.Id;
    }

    public async Task ArchiveCouponAsync(
        Guid id,
        Guid actorId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var coupon = await context.DiscountCoupons.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Coupon not found.");

        coupon.IsArchived = true;
        coupon.IsActive = false;
        AddAudit(
            "Coupon.Archived",
            nameof(DiscountCoupon),
            id.ToString(),
            actorId,
            ipAddress,
            null);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminAuditItem>> GetAuditAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, MaximumPageSize);

        return await context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(log => log.CreatedAtUtc)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(log => new AdminAuditItem(
                log.Action,
                log.EntityName,
                log.EntityId,
                log.DetailsJson,
                log.IpAddress,
                log.CreatedAtUtc,
                log.UserId))
            .ToListAsync(cancellationToken);
    }

    public async Task RecordAuditAsync(
        string action,
        string entity,
        string entityId,
        Guid actorId,
        string ipAddress,
        object? details,
        CancellationToken cancellationToken = default)
    {
        AddAudit(action, entity, entityId, actorId, ipAddress, details);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static Expression<Func<DiscountCoupon, AdminCouponModel>> CouponProjection =>
        coupon => new AdminCouponModel
        {
            Id = coupon.Id,
            Code = coupon.Code,
            Type = coupon.Type.ToString(),
            Value = coupon.Value,
            MinimumOrderAmount = coupon.MinimumOrderAmount,
            UsageLimit = coupon.UsageLimit,
            UsageCount = coupon.UsageCount,
            StartsAtUtc = coupon.StartsAtUtc,
            EndsAtUtc = coupon.EndsAtUtc,
            IsActive = coupon.IsActive
        };

    private static void UpdateShipment(Order order, ShipmentStatus status)
    {
        var shipment = order.Shipments.FirstOrDefault();
        if (shipment is null)
        {
            return;
        }

        shipment.Status = status;
        if (status == ShipmentStatus.Shipped)
        {
            shipment.ShippedAtUtc ??= DateTime.UtcNow;
        }

        if (status == ShipmentStatus.Delivered)
        {
            shipment.DeliveredAtUtc ??= DateTime.UtcNow;
        }
    }

    private static void UpdateCashPayment(Order order, OrderStatus status)
    {
        var payment = order.Payments
            .OrderByDescending(candidate => candidate.CreatedAtUtc)
            .FirstOrDefault();

        if (payment is null || status != OrderStatus.Delivered)
        {
            return;
        }

        payment.Status = PaymentStatus.Paid;
        payment.PaidAtUtc ??= DateTime.UtcNow;
    }

    private void AddAudit(
        string action,
        string entity,
        string id,
        Guid actorId,
        string ipAddress,
        object? details)
    {
        context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = action,
            EntityName = entity,
            EntityId = id,
            IpAddress = ipAddress,
            DetailsJson = details is null
                ? string.Empty
                : JsonSerializer.Serialize(details)
        });
    }
}
