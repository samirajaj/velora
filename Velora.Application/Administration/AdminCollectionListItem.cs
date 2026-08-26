namespace Velora.Application.Administration;

public sealed record AdminCollectionListItem(Guid Id, string Name, string Slug, int ProductCount, bool IsFeatured, bool IsArchived, DateTime? PublishAtUtc);
