# Cluely Backend Release Candidate — RC1

**Date:** 2026-07-12  
**Branch:** `feature/backend-release-candidate`  
**Scope:** Production readiness only; no frontend or business-feature changes.

## Release assessment

RC1 is suitable for frontend integration and a controlled single-instance production deployment after environment configuration and database migrations. No Critical or High-severity code defects remain from this review.

## Security review

Resolved:

- Refresh-token rotation is atomic; concurrent replay permits one successful replacement only.
- The moderator authorization seam uses a deployment-controlled user-ID allow-list instead of a deny-all placeholder.
- Authentication endpoints have a stricter fixed-window rate limit; all endpoints have a global in-process limit.
- CORS is deny-by-default and accepts only configured exact origins.
- Kestrel request bodies default to a 1 MiB maximum.
- API responses include anti-sniffing, frame, referrer, permissions, and API-safe CSP headers.
- Unexpected exceptions return generic ProblemDetails and never expose exception messages or stack traces.
- Failed-login logs no longer contain email addresses.

Verified:

- JWT issuer, audience, signing key, lifetime, and 30-second clock skew validation.
- Refresh tokens are random, hashed at rest, rotated, and revoked on logout.
- Room participant identity is resolved server-side; clients cannot authorize with participant IDs.
- Dictionary visibility is filtered server-side. Owners see history; shared/public viewers see only the current version.
- EF Core parameterizes database queries; no raw SQL was found.
- Metadata is JSON-encoded by ASP.NET Core; no HTML rendering occurs in the backend.

## Production readiness

- Health checks cover primary SQL connectivity, Identity DB connectivity, content-table queryability, and SignalR delivery registration.
- Request telemetry records CorrelationId (log context), UserId, RoomId or DictionaryId when present, status, and elapsed milliseconds without bodies or tokens.
- HSTS is enabled outside Development; TLS termination remains a deployment requirement.
- `openapi.json` is generated during every API build and validated against ApiExplorer in integration tests.

## Performance review

No optimization was applied without evidence.

- Discovery summary queries use `AsNoTracking` and SQL projection.
- Visibility checks execute server-side with indexed owner, visibility, and grantee columns.
- Detail/version reads load one projected snapshot and deserialize once; no N+1 query path was found.
- Repository aggregate loads are `AsNoTracking`; writes use optimistic concurrency.
- SignalR builds one internal projection per broadcast, then filters per connection.
- Publish outcome lookup uses the idempotency-key primary key.

## Architecture audit

The existing architecture suite verifies Domain isolation, API-to-Domain restrictions, DbContext placement, immutable collection exposure, no reflection/serialization attributes in Content Domain, thin controllers, Application-to-Infrastructure separation, and content controller dependencies. All architecture tests pass.

## Accepted debt and operating limits

- **TD-020:** discovery and version history are unpaginated. Global rate/request limits reduce abuse, but large catalogs can increase latency and memory. Pagination requires a frozen-contract revision.
- **TD-010:** non-publish content mutations do not all have command idempotency keys.
- **TD-008:** some domain exceptions still map through a broad conflict fallback.
- Moderator assignment is configuration-based rather than persisted role administration.
- Rate limiting and SignalR connection state are process-local. Multi-instance production requires edge-distributed rate limiting, sticky sessions, and a SignalR scale-out design.
- Identity and application contexts share one SQL connection string while retaining separate DbContexts.
- Email verification is not implemented.
- The development JWT key is checked in for local use and must be overridden from a production secret manager.

## Frontend handoff

- Consume `src/Cluely.Api/openapi.json`.
- Treat RFC 7807 `code` and `correlationId` as the stable error envelope.
- Send `Authorization: Bearer` on protected operations.
- Send a UUID `Idempotency-Key` on every retryable create/clone request and always on publish.
- Configure the exact frontend origin through `Cors:AllowedOrigins`.

## Validation

Release validation requires:

```bash
dotnet restore
dotnet build
dotnet test tests/Cluely.UnitTests --no-build
dotnet test tests/Cluely.ArchitectureTests --no-build
dotnet test tests/Cluely.IntegrationTests --no-build
```

The release commit records the final counts and zero-warning/zero-skip result.
