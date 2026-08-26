namespace Velora.Features.Media;

public sealed class ProductImageValidator : IProductImageValidator
{
    public const int MaximumFileCount = 8;
    public const long MaximumFileSize = 10_485_760;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public async Task<bool> IsValidAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file.Length is <= 0 or > MaximumFileSize ||
            !AllowedContentTypes.Contains(file.ContentType))
        {
            return false;
        }

        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header, cancellationToken);

        return bytesRead >= 3 && IsJpeg(header) ||
               bytesRead >= 8 && IsPng(header) ||
               bytesRead >= 12 && IsWebP(header);
    }

    private static bool IsJpeg(IReadOnlyList<byte> header) =>
        header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;

    private static bool IsPng(IReadOnlyList<byte> header) =>
        header[0] == 0x89 &&
        header[1] == 0x50 &&
        header[2] == 0x4E &&
        header[3] == 0x47 &&
        header[4] == 0x0D &&
        header[5] == 0x0A &&
        header[6] == 0x1A &&
        header[7] == 0x0A;

    private static bool IsWebP(IReadOnlyList<byte> header) =>
        header[0] == 0x52 &&
        header[1] == 0x49 &&
        header[2] == 0x46 &&
        header[3] == 0x46 &&
        header[8] == 0x57 &&
        header[9] == 0x45 &&
        header[10] == 0x42 &&
        header[11] == 0x50;
}
