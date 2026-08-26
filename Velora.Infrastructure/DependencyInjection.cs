using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Velora.Application.Catalog;
using Velora.Application.Administration;
using Velora.Application.Media;
using Velora.Infrastructure.Catalog;
using Velora.Infrastructure.Media;
using Velora.Infrastructure.Persistence;
using Velora.Infrastructure.Identity;
using Velora.Application.Customers;
using Velora.Application.Commerce;
using Velora.Application.Communication;
using Velora.Infrastructure.Customers;
using Velora.Infrastructure.Commerce;
using Velora.Infrastructure.Communication;
using Velora.Infrastructure.Health;
using Velora.Infrastructure.Observability;

namespace Velora.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = ".Velora.Identity";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            options.LoginPath = "/account/login";
            options.AccessDeniedPath = "/account/access-denied";
            options.ExpireTimeSpan = TimeSpan.FromDays(14);
            options.SlidingExpiration = true;
        });
        services.AddAuthorizationBuilder()
            .AddPolicy(AppPolicies.ManageCatalog, policy => policy.RequireRole(AppRoles.Admin).RequireClaim("amr", "mfa"))
            .AddPolicy(AppPolicies.ManageOrders, policy => policy.RequireRole(AppRoles.Admin).RequireClaim("amr", "mfa"))
            .AddPolicy(AppPolicies.ManageUsers, policy => policy.RequireRole(AppRoles.Admin).RequireClaim("amr", "mfa"))
            .AddPolicy(AppPolicies.ViewAudit, policy => policy.RequireRole(AppRoles.Admin).RequireClaim("amr", "mfa"));
        services.Configure<CloudinaryOptions>(configuration.GetSection(CloudinaryOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddScoped<IProductCatalogService, ProductCatalogService>();
        services.AddScoped<IAdminCatalogService, AdminCatalogService>();
        services.AddScoped<IMediaStorage, CloudinaryMediaStorage>();
        services.AddScoped<ICustomerAccountService, CustomerAccountService>();
        services.AddScoped<ICheckoutService, CheckoutService>();
        services.AddScoped<IAdminCommerceService, AdminCommerceService>();
        services.AddScoped<ITransactionalEmailSender, SmtpEmailSender>();
        services.AddSingleton<OrderMetrics>();
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("sql", tags: ["ready"])
            .AddCheck<CloudinaryHealthCheck>("cloudinary", tags: ["ready"]);
        return services;
    }
}
