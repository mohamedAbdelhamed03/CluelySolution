# OpenAPI Usage

`dotnet build` generates `src/Cluely.Api/openapi.json` from the same Swashbuckle configuration used by the Development Swagger UI.

## Generate

```bash
dotnet build src/Cluely.Api/Cluely.Api.csproj
```

The resulting OpenAPI 3.0 artifact documents every controller operation, operation-level bearer requirements, ProblemDetails responses, XML summaries, and idempotency headers.

## Frontend workflow

1. Build the backend from the reviewed commit.
2. Copy or publish `src/Cluely.Api/openapi.json` as a CI artifact.
3. Generate the frontend client from that immutable artifact.
4. Fail CI if generation produces an unreviewed contract diff.

Publish requires a UUID `Idempotency-Key`; generated clients must expose it as a required header. Create and clone expose the same header as optional but retry-safe clients should always supply one.

Swagger UI remains available at `/swagger` in Development only.
