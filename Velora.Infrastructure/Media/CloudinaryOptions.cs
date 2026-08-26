namespace Velora.Infrastructure.Media;

public sealed class CloudinaryOptions
{
    public const string SectionName = "Cloudinary";
    public string Url { get; set; } = string.Empty;
    public string Folder { get; set; } = "velora/products";
}
