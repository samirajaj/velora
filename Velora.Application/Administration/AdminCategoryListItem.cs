namespace Velora.Application.Administration;

public sealed record AdminCategoryListItem(Guid Id, string Name, string Slug, int ProductCount, bool IsActive, bool IsArchived);
