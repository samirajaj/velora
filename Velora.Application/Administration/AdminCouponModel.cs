namespace Velora.Application.Administration;

public sealed class AdminCouponModel
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = "Percentage";
    public decimal Value { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public int? UsageLimit { get; set; }
    public int UsageCount { get; set; }
    public DateTime StartsAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndsAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
}
