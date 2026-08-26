using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Velora.Configuration;
using Velora.Features.Cart;
using Velora.Features.Media;
using Velora.Infrastructure;
using Velora.Infrastructure.Identity;
using Velora.Infrastructure.Observability;
using Velora.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Application services
builder.Services.AddControllersWithViews();
builder.Services.Configure<SiteOptions>(
    builder.Configuration.GetSection(SiteOptions.SectionName));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICartService, SessionCartService>();
builder.Services.AddSingleton<IProductImageValidator, ProductImageValidator>();

// Session and cart
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".Velora.Cart";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromDays(14);
});

// Output caching
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy(
        "Storefront",
        policy => policy
            .Expire(TimeSpan.FromSeconds(30))
            .SetVaryByHeader("Cookie")
            .SetVaryByQuery("*"));
});

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 8;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("checkout", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromMinutes(2);
        limiter.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("newsletter", limiter =>
    {
        limiter.PermitLimit = 3;
        limiter.Window = TimeSpan.FromMinutes(10);
        limiter.QueueLimit = 0;
    });
});

// OpenTelemetry
var telemetry = builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService(builder.Environment.ApplicationName))
    .WithTracing(tracing =>
        tracing
            .AddAspNetCoreInstrumentation(options =>
                options.Filter = context =>
                    !context.Request.Path.StartsWithSegments("/health"))
            .AddHttpClientInstrumentation(options =>
                options.RecordException = true)
            .AddSqlClientInstrumentation(options =>
                options.RecordException = true)
            .AddSource(OrderMetrics.OrderActivitySource))
    .WithMetrics(metrics =>
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter(OrderMetrics.MeterName));

// Export telemetry only when an OTLP endpoint is configured
if (!string.IsNullOrWhiteSpace(
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
{
    telemetry
        .WithTracing(tracing =>
            tracing.AddOtlpExporter())
        .WithMetrics(metrics =>
            metrics.AddOtlpExporter());

    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeScopes = true;
        logging.IncludeFormattedMessage = true;
        logging.AddOtlpExporter();
    });
}

var app = builder.Build();

// Error handling and HTTPS
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Security headers
app.Use(async (context, next) =>
{
    // Generate a unique CSP nonce for this request
    var nonce = Convert.ToBase64String(
        System.Security.Cryptography.RandomNumberGenerator.GetBytes(18));

    context.Items["CspNonce"] = nonce;

    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] =
        "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] =
        "camera=(), microphone=(), geolocation=()";

    context.Response.Headers.ContentSecurityPolicy =
        $"default-src 'self'; " +
        $"img-src 'self' https://res.cloudinary.com https://images.unsplash.com data:; " +
        $"style-src 'self' https://fonts.googleapis.com 'unsafe-inline'; " +
        $"font-src 'self' https://fonts.gstatic.com; " +
        $"script-src 'self' 'nonce-{nonce}'; " +
        $"form-action 'self'; " +
        $"frame-ancestors 'none'; " +
        $"base-uri 'self'";

    await next();
});

// Request pipeline
app.UseRouting();
app.UseRateLimiter();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

// Static assets
app.MapStaticAssets();

// Health checks
app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = _ => false
    });

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = registration =>
            registration.Tags.Contains("ready")
    });

// MVC routes
app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Seed development catalog data
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();

    var db = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    await CatalogSeed.InitializeAsync(db);
}

// Seed required identity data
await using (var identityScope = app.Services.CreateAsyncScope())
{
    await IdentitySeed.InitializeAsync(
        identityScope.ServiceProvider,
        builder.Configuration);
}

// Start application
app.Run();
