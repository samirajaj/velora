using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Velora.Infrastructure.Media;

namespace Velora.Infrastructure.Health;

public sealed class CloudinaryHealthCheck(IOptions<CloudinaryOptions> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var result = string.IsNullOrWhiteSpace(options.Value.Url)
            ? HealthCheckResult.Degraded("Cloudinary is not configured.")
            : HealthCheckResult.Healthy("Cloudinary configuration is present.");

        return Task.FromResult(result);
    }
}
