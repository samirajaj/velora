using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using Velora.Application.Media;

namespace Velora.Infrastructure.Media;

internal sealed class CloudinaryMediaStorage(IOptions<CloudinaryOptions> options) : IMediaStorage
{
    private readonly CloudinaryOptions _options = options.Value;

    public async Task<MediaUploadResult> UploadImageAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var cloudinary = CreateClient();
        var result = await cloudinary.UploadAsync(new ImageUploadParams
        {
            File = new FileDescription(fileName, content),
            Folder = _options.Folder,
            UseFilename = true,
            UniqueFilename = true,
            Transformation = new Transformation().Width(1600).Height(2000).Crop("limit").Quality("auto").FetchFormat("auto")
        }, cancellationToken);

        if (result.Error is not null)
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");

        return new MediaUploadResult(result.SecureUrl.AbsoluteUri, result.PublicId);
    }

    public async Task DeleteImageAsync(string publicId, CancellationToken cancellationToken = default)
    {
        var result = await CreateClient().DestroyAsync(new DeletionParams(publicId));
        if (result.Result is not ("ok" or "not found"))
            throw new InvalidOperationException($"Cloudinary deletion failed: {result.Result}");
    }

    private Cloudinary CreateClient()
    {
        if (string.IsNullOrWhiteSpace(_options.Url))
            throw new InvalidOperationException("Cloudinary:Url is not configured. Set Cloudinary__Url in the environment.");

        var client = new Cloudinary(_options.Url) { Api = { Secure = true } };
        return client;
    }
}
