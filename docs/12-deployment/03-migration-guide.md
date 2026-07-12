# Database Migration Guide

Cluely uses two EF Core contexts against the configured SQL connection:

- `CluelyDbContext`: rooms, content snapshots, share grants, and content command outcomes.
- `IdentityDbContext`: users, refresh tokens, and participant bindings.

Apply migrations from the repository root:

```bash
dotnet ef database update \
  --project src/Cluely.Infrastructure \
  --startup-project src/Cluely.Api \
  --context CluelyDbContext

dotnet ef database update \
  --project src/Cluely.Infrastructure \
  --startup-project src/Cluely.Api \
  --context IdentityDbContext
```

## RC1 migration state

`20260712141831_AddContentCommandOutcomes` must be applied before accepting publish traffic. It stores deterministic publish results keyed by `Idempotency-Key`; omitting it causes publish requests to fail.

## Production procedure

1. Back up the database.
2. Review generated SQL with `dotnet ef migrations script`.
3. Apply migrations once as a deployment step, not concurrently from every application replica.
4. Start the API and verify `/api/health`.
5. Roll back the application before attempting a database rollback. Review reverse migration SQL for data loss.
