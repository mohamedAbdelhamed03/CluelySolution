# Backend RC1 Release Notes

## Reliability and security

- Atomic refresh-token rotation prevents concurrent replay.
- Configured moderator allow-list replaces the unavailable moderator implementation.
- SQL, Identity, Content, and SignalR health checks replace the process-only health response.
- Global and authentication-specific rate limits, exact-origin CORS, request-size limits, HSTS, and security headers are enabled.
- Unexpected exception details are sanitized.
- Structured request telemetry includes identity/scope and elapsed time without sensitive payloads.

## API contract

- Publish requires a UUID `Idempotency-Key`.
- Build emits `src/Cluely.Api/openapi.json`.
- OpenAPI applies bearer security only to authorized operations and documents idempotency headers.

## Database

Apply both EF contexts. RC1 requires the previously introduced `AddContentCommandOutcomes` migration for publish idempotency.

## Compatibility

Business behavior and response DTOs are unchanged. Requiring `Idempotency-Key` on publish formalizes the stabilization contract introduced to close TD-017.

## Known limitations

See the accepted-debt section in the [RC1 report](backend-release-candidate-report.md), especially unpaginated discovery, process-local rate limiting/SignalR state, and configuration-based moderator assignment.
