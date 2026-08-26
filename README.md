# Velora

Velora is a modern fashion storefront built with ASP.NET Core MVC, .NET 10, EF Core 10, SQL Server, and Cloudinary.

## Architecture

The solution follows Clean Architecture as a pragmatic modular monolith:

- `Velora.Domain` contains business entities and has no infrastructure dependencies.
- `Velora.Application` defines use-case contracts and immutable data-transfer models.
- `Velora.Infrastructure` implements persistence and media storage with EF Core and Cloudinary.
- `Velora` is the MVC presentation layer. Customer features are grouped vertically by feature.

Dependencies point inward: Web → Application/Infrastructure → Application/Domain. Controllers depend on application interfaces rather than `DbContext`.

## Local setup

1. Install the .NET SDK selected by `global.json`.
2. Initialize the development secrets shown below.
3. Restore and run:

```powershell
dotnet restore
dotnet tool restore
dotnet run --project Velora
```

In Development, pending migrations are applied and starter catalog data is inserted when the catalog is empty.

## Database changes

Create a migration after changing domain entities or EF configurations:

```powershell
dotnet ef migrations add MigrationName --project Velora.Infrastructure --startup-project Velora --output-dir Persistence/Migrations
```

For production, apply migrations as a deployment step rather than relying on application startup:

```powershell
dotnet ef database update --project Velora.Infrastructure --startup-project Velora
```

## Configuration and secrets

Real credentials are intentionally excluded from tracked settings. Use ASP.NET User Secrets locally:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_SQL_CONNECTION" --project Velora
dotnet user-secrets set "Cloudinary:Url" "cloudinary://KEY:SECRET@CLOUD" --project Velora
```

To create the initial administrator securely, set these once and start the application:

```powershell
dotnet user-secrets set "SeedAdmin:Email" "admin@example.com" --project Velora
dotnet user-secrets set "SeedAdmin:Password" "A-STRONG-UNIQUE-PASSWORD" --project Velora
```

In production, provide the equivalent environment variables `ConnectionStrings__DefaultConnection`, `Cloudinary__Url`, and optionally `Cloudinary__Folder`.

Rotate any credential that has been shared in chat or another non-secret channel before production launch.

## Implemented storefront slice

- Editorial, responsive home page using the supplied Velora artwork and Bodoni Moda
- Searchable, category-filtered, sortable, paginated catalog
- Product detail pages with stock variants
- Server-side session cart with anti-forgery protection
- EF Core schema, migration, and starter catalog
- Cloudinary upload/delete implementation behind `IMediaStorage`
- ASP.NET Core Identity with Customer and Admin roles
- Customer registration, login, lockout protection, and session authentication
- Role-protected admin dashboard and customer listing
- SEO description and Open Graph metadata

Checkout, customer accounts, order management, and an authenticated admin area are intentionally left for the next vertical slices.
