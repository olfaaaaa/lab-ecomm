# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Requires the .NET 10 SDK (`dotnet --version` must print `10.x`). Docker is not needed.

```bash
dotnet dev-certs https --trust          # one-time; without it the Aspire dashboard fails with UntrustedRoot
dotnet run --project src/ECommerce.AppHost   # starts every service + the Aspire dashboard
dotnet build ECommerce.slnx             # not run to validate this file; the solution uses the .slnx format
```

- Per the README (not verified at runtime): open the `Login URL` printed in the terminal (the port changes on every run) and do not open the `OTLP/gRPC` URL. Ctrl+C in the AppHost terminal stops everything.
- There are no tests, linter config, Dockerfile or CI in the repo, so there is no test or lint command. Do not assume a test project exists; adding one means creating it and adding it to `ECommerce.slnx`.
- Dev-mode OpenAPI is mapped on Catalog and Ordering (`MapOpenApi()`). The README gives the path as `/openapi/v1.json`.

## Architecture

Microservices orchestrated by .NET Aspire. Request flow: `Web (Blazor Server) -> Gateway (YARP) -> Catalog | Ordering`, and `Ordering -> Catalog` directly.

- **Run through the AppHost.** [AppHost.cs](src/ECommerce.AppHost/AppHost.cs) declares the resource names (`catalog`, `ordering`, `gateway`, `web`), the `WithReference`/`WaitFor` dependencies, and is what injects service addresses. Running a service project on its own will not resolve other services.
- **Service discovery, no hard-coded URLs.** Clients use logical addresses such as `https+http://catalog` and `https+http://gateway`
  ([Ordering Program.cs](src/ECommerce.Ordering.Api/Program.cs), [Web Program.cs](src/ECommerce.Web/Program.cs)). Per Aspire's standard behavior (not defined in this repo), they are resolved at runtime from `services__<name>__*` environment variables that the AppHost injects.
  The Gateway uses `AddServiceDiscoveryDestinationResolver()` for the same purpose. A new service-to-service call needs a `WithReference` in `AppHost.cs` and a logical-name `BaseAddress`.
- **ServiceDefaults** ([Extensions.cs](src/ECommerce.ServiceDefaults/Extensions.cs)) is referenced by Web, Gateway, Catalog and Ordering (not by the AppHost) and called as `builder.AddServiceDefaults()`. It configures OpenTelemetry, health checks, service discovery and the standard HTTP resilience handler for all `HttpClient`s. `/health` and `/alive` are only mapped in Development.
- **Gateway routing** lives entirely in [Gateway appsettings.json](src/ECommerce.Gateway/appsettings.json): `/catalog/{**}` and `/ordering/{**}` with `PathRemovePrefix`. Web calls paths like `/ordering/api/orders`, which reach the service as `/api/orders`.
- **Order creation** ([OrderingEndpoints.cs](src/ECommerce.Ordering.Api/Endpoints/OrderingEndpoints.cs)): for each line, Ordering makes a synchronous `GET /api/products/{id}` to Catalog through `CatalogServiceClient`. It snapshots the product name and price into `OrderItem`, sets the status to `Confirmed`, and saves.
  Stock is not checked or decremented, and quantities are not validated.
- **Persistence.** Each service has its own EF Core `InMemory` database (`catalog`, `ordering`), created with `EnsureCreated()` at startup. There are no migrations. Catalog seeds 4 products via `HasData` in `CatalogDbContext`. Data is lost on every restart.
- **API style.** Minimal APIs: each service has an `Endpoints/*Endpoints.cs` extension method that is called from `Program.cs`. Ordering serializes enums as strings.
- **Web DTOs** are duplicated in [Web/Services](src/ECommerce.Web/Services) rather than shared with the APIs. A change to an API contract must be mirrored by hand.

## Conventions

- The README is in French and calls the repo a deliberately minimal training application ("fil rouge"). Tests, a real database (PostgreSQL), containers and CI are expected to be added later.
- `bin/` and `obj/` are gitignored and not tracked.
