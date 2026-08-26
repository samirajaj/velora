namespace Velora.Application.Media;

public interface IMediaStorage
{
    Task<MediaUploadResult> UploadImageAsync(Stream content, string fileName, CancellationToken cancellationToken = default);
    Task DeleteImageAsync(string publicId, CancellationToken cancellationToken = default);
}
