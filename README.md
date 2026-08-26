# Velora

Velora is a fashion commerce application built with ASP.NET Core MVC, .NET 10, EF Core 10, SQL Server, ASP.NET Core Identity, and Cloudinary.

## Architecture

The solution is a Clean Architecture modular monolith:

- `Velora.Domain` contains catalog, customer, order, payment, shipment, discount, inventory, and audit entities.
- `Velora.Application` defines use-case contracts and transport models.
- `Velora.Infrastructure` implements EF Core persistence, Identity, Cloudinary media, SMTP email, health checks, and commerce services.
- `Velora` is the MVC presentation layer, organized into storefront features and a protected Admin area.

The web layer calls application interfaces; business records and persistence behavior stay out of controllers.

## Implemented commerce features

- Product/category administration, variants, SKUs, stock, pricing, sale pricing, publishing, featuring, and soft archive
- Multiple Cloudinary images, variant assignment, primary selection, drag ordering, validation, and remote deletion
- Search, pagination, category/price/color/size/availability filters, related products, wishlist, and recently viewed tracking
- Confirmed-email registration, password recovery, profiles, delivery addresses, order history, persistent cart merge, and TOTP administrator 2FA
- Server-authoritative cash-on-delivery checkout with serializable inventory updates and immutable order-item price snapshots
- Orders, payments, shipments, status history, coupons, fulfillment management, low-stock reporting, customer activation, roles, and audit history
- Rate limits, CSP and security headers, health endpoints, OpenTelemetry traces/metrics/log export, sitemap, robots.txt, canonical URLs, and product structured data

## Local setup

Install the SDK selected by `global.json`, then configure secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_SQL_CONNECTION" --project Velora
dotnet user-secrets set "Cloudinary:Url" "cloudinary://KEY:SECRET@CLOUD" --project Velora
dotnet user-secrets set "Email:Host" "smtp.example.com" --project Velora
dotnet user-secrets set "Email:UserName" "SMTP_USER" --project Velora
dotnet user-secrets set "Email:Password" "SMTP_PASSWORD" --project Velora
dotnet user-secrets set "SeedAdmin:Email" "admin@example.com" --project Velora
dotnet user-secrets set "SeedAdmin:Password" "A-STRONG-UNIQUE-PASSWORD" --project Velora
dotnet tool restore
dotnet restore
dotnet run --project Velora
```

In Development only, startup applies pending migrations and inserts starter catalog data. Production startup never applies migrations.

## Database deployment

Create and inspect migrations locally:

```powershell
dotnet ef migrations add MigrationName --project Velora.Infrastructure --startup-project Velora --output-dir Persistence/Migrations
```

Apply reviewed migrations as an explicit deployment step:

```powershell
dotnet ef database update --project Velora.Infrastructure --startup-project Velora
```

## Production configuration

Use the hosting provider's secret manager for `ConnectionStrings__DefaultConnection`, `Cloudinary__Url`, SMTP values, and seed credentials. Set `OTEL_EXPORTER_OTLP_ENDPOINT` to export telemetry to an OTLP collector. Readiness is exposed at `/health/ready`; liveness is at `/health/live`.

Rotate any credential that has appeared in chat, source control, logs, or other non-secret channels before launch.
