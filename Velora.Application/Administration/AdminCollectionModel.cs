namespace Velora.Application.Administration;

public sealed class AdminCollectionModel
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? PublishAtUtc { get; set; }
}
