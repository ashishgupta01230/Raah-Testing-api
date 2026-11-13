# Raah-api

Coding conventions for this repo. For what the system does and the business rules, see [`README.md`](README.md) and [`docs/requirements/`](docs/requirements/) — don't duplicate that here. Development proceeds phase by phase — check with the user before assuming a later phase's scope is already in play.

## Rule: no global/generic code

Do not write broad, generic, "might be reused someday" shared abstractions. Keep implementations scoped specifically to the feature being built. Three similar lines in two places beats a premature shared helper/service. Only extract a shared abstraction once an actual second caller needs it.

## Solution layout

```
src/
  BuildingBlocks/          Shared cross-cutting libraries (Domain, Application, Persistence,
                            Logger, Configuration, QueryBuilder, Neo4j config)
  Services/Binsera/         The main service (project name "Binsera", predates the Raah rebrand)
    Binsera.API/             ASP.NET Core Web API — controllers, background services, DI wiring
    Binsera.Application/     Application/business logic services
    Binsera.Core/             Domain entities, domain events, and input/DTO models
    Binsera.Persistence/      EF Core DbContext, entity configurations, migrations, interceptors
  DatabaseQuery/DbQuery/     Legacy SQL Server Database Project (raw stored procedures/scripts) —
                              NOT buildable via `dotnet build`/`dotnet-cli` (needs VS/SSDT tooling);
                              a `DbQuery.sqlproj` build error in plain `dotnet build Raah.sln`
                              is expected and unrelated to your changes — build individual .csproj
                              files instead (e.g. `dotnet build src/Services/Binsera/Binsera.API/Binsera.API.csproj`).
docs/
  requirements/              Business rules docs (source of truth)
  postman/                  Postman collections for manual/API testing
```

## Dependency injection — `[AutoRegister]`, not manual `AddScoped`

There is no MediatR/CQRS in this codebase — plain injected services throughout. Classes are registered in DI via a custom attribute convention, not manual `services.AddScoped<>()` calls:

```csharp
[AutoRegister(includeInterfaces: true)]
public class MemberRegistrationService(...) : IMemberRegistrationService { ... }
```

`AddAutoRegisteredServices()` (`BuildingBlocks/Application/Extensions/ServiceCollectionExtensions.cs`) scans every loaded assembly named `Binsera.*` for `[AutoRegister]`-decorated classes and registers each against its interfaces (or itself). When adding a new service, follow this pattern instead of hand-wiring DI in `Program.cs`.

## Non-2xx responses without a body — use `StatusCode(n)`, not `Forbid()`/`Challenge()`

Every controller action's result passes through a global `ResponseFilterAttribute` that rewraps `Ok(...)`/`BadRequest(...)`/etc. into the app's `{ data, errorCode, ... }` envelope. It only special-cases (leaves untouched) results that are `FileResult`, `StatusCodeResult`, or `EmptyResult` — anything else, including `ForbidResult`/`ChallengeResult`, gets silently swallowed and rewritten as a **200 OK with `data: null`**, discarding the intended status code entirely. `Unauthorized(...)`/`BadRequest(...)` survive because their object-result variants (`UnauthorizedObjectResult`, `BadRequestObjectResult`) are `ObjectResult`s, which the filter also preserves correctly. For a plain reject with no body (e.g. an admin-only check), use `return StatusCode(403);` (a `StatusCodeResult`) — never `Forbid()`.

## Validation — none today; don't assume FluentValidation

There is no validation framework in use anywhere in `src/` (no FluentValidation, no `IValidator`). Input validation is ad hoc/manual where it exists at all — e.g. sponsor-existence is checked via a separate pre-check endpoint (`RegistrationValidationController`), not enforced server-side at registration time. Treat this as a known gap, not a convention to imitate uncritically — but don't introduce a validation framework as a drive-by change either; that's a decision for the user.

## Migrations

- Timestamp-prefixed PascalCase names (`yyyyMMddHHmmss_DescriptiveName`), paired `.cs` + `.Designer.cs`, in `Binsera.Persistence/Migrations/`.
- `Up()` creates tables with a `long Id` identity PK, `decimal` columns via `.HasPrecision(...)`, audit columns (`CreatedDate/CreatedBy/ModifiedDate/ModifiedBy`), `IsDeleted` bit, followed by per-column `CreateIndex` calls. `Down()` just drops the created tables.
- **`Member`'s table (`member_Master`) is excluded from EF migrations** (`MemberConfiguration.cs`: `SetIsTableExcludedFromMigrations(true)`) — it's treated as a pre-existing legacy table whose schema EF doesn't manage. **Keep reusing `member_Master`/`Member` as-is** (confirmed decision) — do not fork or replace it with a fresh Member table. New feature-specific data (placement, levels, wallets, etc.) belongs in **new, normal EF-managed tables** that link back to `Member` by FK, not as new columns bolted onto `member_Master`.
- For a new business-rule config table (e.g. a future level/pool-rate table), follow `PerformanceLevelSlabConfiguration.cs`'s pattern: `Id` PK, `IsActive`/`IsDeleted` flags, `HasIndex(x => x.Level).IsUnique()`, seed rows via `HasData(...)`.

## Background jobs

Plain `BackgroundService` subclasses (no external job framework), using the `Cronos` package purely as a cron-expression parser, config-driven from an `IConfiguration` section (`Enabled`/`Cron`/`TimeZone`). See `Binsera.API/BackgroundServices/MonthlyCommissionCalculationJob.cs` for the shape — own `while` loop computing `_cron.GetNextOccurrence(...)`, resolving scoped services via `IServiceScopeFactory` inside each tick. Register new jobs the same way `Program.cs` does: `builder.Services.AddHostedService<YourJob>();`.
