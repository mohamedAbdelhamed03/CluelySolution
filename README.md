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
- (Optional) Docker for containerized development

## Building the Solution
To build the entire solution:
```bash
dotnet build
```

## Running the Tests
To run all tests:
```bash
dotnet test
```

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
