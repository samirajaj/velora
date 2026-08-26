namespace Velora.Features.Media;

public interface IProductImageValidator
{
    Task<bool> IsValidAsync(IFormFile file, CancellationToken cancellationToken = default);
}
