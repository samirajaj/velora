namespace Velora.Application.Administration;

public sealed record AdminDashboardStats(int ActiveProducts, int LowStockVariants, int Customers, int OpenOrders, decimal Revenue, IReadOnlyList<AdminOrderListItem> RecentOrders);
