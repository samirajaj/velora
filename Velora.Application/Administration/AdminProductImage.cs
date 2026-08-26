namespace Velora.Application.Administration;

public sealed record AdminProductImage(Guid Id, string Url, string PublicId, string AltText, int DisplayOrder, bool IsPrimary, Guid? VariantId);
