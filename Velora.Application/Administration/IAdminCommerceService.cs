namespace Velora.Application.Administration;

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
