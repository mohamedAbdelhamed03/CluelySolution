# Cluely

A modular monolith application built with .NET 10 following Clean Architecture, Domain-Driven Design, and Vertical Slice Architecture.

## Project Structure

### Source Code (`src/`)
- **Cluely.Api**: The entry point of the application, exposes REST APIs and SignalR hubs.
- **Cluely.Application**: Contains application use cases, validation (via FluentValidation), and vertical slices.
- **Cluely.Domain**: The core of the application, contains aggregate roots, entities, value objects, domain events, and domain errors - completely framework-free.
- **Cluely.Infrastructure**: Implements technical concerns like persistence, logging, external service integrations, and middleware.

### Tests (`tests/`)
- **Cluely.UnitTests**: Unit tests for domain and application logic.
- **Cluely.IntegrationTests**: Integration tests, including API tests.
- **Cluely.ArchitectureTests**: Automated tests that enforce architectural rules (e.g., Domain layer has no external dependencies, API layer does not reference Domain directly).

## Prerequisites
- .NET 10 SDK
- SQL Server or SQL Server LocalDB

## Building the Solution
To build the entire solution:
```bash
dotnet build
```

The build also generates `src/Cluely.Api/openapi.json` for frontend client generation.

## Running the Tests
To run all tests:
```bash
dotnet test
```

## Local Configuration

Development defaults live in `src/Cluely.Api/appsettings.json`. Production deployments must override:

- `ConnectionStrings__CluelyDb`
- `Jwt__Issuer`, `Jwt__Audience`, and `Jwt__SigningKey`
- `Cors__AllowedOrigins__0` (and additional indexed origins)
- `ContentModeration__ModeratorUserIds__0` (and additional moderator IDs)

Do not deploy the development JWT signing key. Apply EF migrations for both `CluelyDbContext` and `IdentityDbContext` before starting the API. See [Deployment](docs/12-deployment/README.md) and the [API guide](src/Cluely.Api/README.md).

## Architectural Rules
This project enforces the following architectural rules via automated tests in `Cluely.ArchitectureTests`:
1. Domain layer must have no references to any other projects.
2. Domain layer must not reference any ASP.NET Core packages.
3. API layer must not reference Domain layer directly.
4. Infrastructure layer must not reference API layer.
5. Controllers must not contain any business logic.
6. No MediatR or AutoMapper dependencies are allowed.

## Technology Stack
- .NET 10
- ASP.NET Core
- Entity Framework Core
- FluentValidation
- Serilog
- xUnit
- FluentAssertions
- NetArchTest
