namespace Velora.Application.Administration;

public sealed record AdminDashboardStats(int ActiveProducts, int LowStockVariants, int Customers, int OpenOrders, decimal Revenue, IReadOnlyList<AdminOrderListItem> RecentOrders);
public sealed record AdminOrderListItem(Guid Id, string Number, string CustomerEmail, string Status, decimal Total, string Currency, int ItemCount, DateTime CreatedAtUtc);
public sealed record AdminOrderLine(string ProductName, string Sku, string Option, int Quantity, decimal LineTotal);
public sealed record AdminOrderDetails(Guid Id, string Number, string CustomerEmail, string RecipientName, string Phone, string Address, string Status, string PaymentStatus, string ShipmentStatus, decimal Total, string Currency, DateTime CreatedAtUtc, IReadOnlyList<AdminOrderLine> Items, IReadOnlyList<string> History);
public sealed class AdminCouponModel { public Guid? Id { get; set; } public string Code { get; set; } = string.Empty; public string Type { get; set; } = "Percentage"; public decimal Value { get; set; } public decimal? MinimumOrderAmount { get; set; } public int? UsageLimit { get; set; } public int UsageCount { get; set; } public DateTime StartsAtUtc { get; set; } = DateTime.UtcNow; public DateTime? EndsAtUtc { get; set; } public bool IsActive { get; set; } = true; }
public sealed record AdminAuditItem(string Action, string EntityName, string EntityId, string Details, string IpAddress, DateTime CreatedAtUtc, Guid? UserId);

public interface IAdminCommerceService
{
    Task<AdminDashboardStats> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminOrderListItem>> GetOrdersAsync(string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<AdminOrderDetails?> GetOrderAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateOrderAsync(Guid id, string status, string shipmentStatus, string note, Guid actorId, string ipAddress, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminCouponModel>> GetCouponsAsync(CancellationToken cancellationToken = default);
    Task<AdminCouponModel?> GetCouponAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> SaveCouponAsync(AdminCouponModel model, Guid actorId, string ipAddress, CancellationToken cancellationToken = default);
    Task ArchiveCouponAsync(Guid id, Guid actorId, string ipAddress, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminAuditItem>> GetAuditAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task RecordAuditAsync(string action, string entity, string entityId, Guid actorId, string ipAddress, object? details, CancellationToken cancellationToken = default);
}
