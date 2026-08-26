namespace Velora.Application.Administration;

public sealed record AdminAuditItem(string Action, string EntityName, string EntityId, string Details, string IpAddress, DateTime CreatedAtUtc, Guid? UserId);
