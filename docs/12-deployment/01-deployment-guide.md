# Backend Deployment Guide

## Release artifact

Publish the API with the .NET 10 SDK:

```bash
dotnet restore
dotnet publish src/Cluely.Api/Cluely.Api.csproj -c Release -o artifacts/api
```

The build produces `src/Cluely.Api/openapi.json`; publish it separately for frontend client generation.

## Required infrastructure

- SQL Server reachable by the API process.
- TLS termination at the reverse proxy or platform ingress.
- Sticky sessions or an external SignalR scale-out provider are required before running more than one API instance. RC1 uses an in-memory connection registry and has no scale-out backplane.

## Deployment sequence

1. Supply production configuration and secrets.
2. Apply both EF Core migration sets as described in the migration guide.
3. Deploy the API artifact.
4. Verify `GET /api/health` returns HTTP 200 and `Healthy`.
5. Verify authentication and one authorized content read.
6. Retain the previous artifact for rollback. Database rollback requires a reviewed reverse migration; never delete content/version data manually.

## Edge requirements

- Redirect HTTP to HTTPS and set HSTS at the edge. The API also enables HSTS outside Development.
- Preserve `X-Correlation-Id` request/response headers.
- Enforce a request-body limit no greater than the API's configured `RequestLimits:MaxBodyBytes`.
- Configure distributed rate limiting at the edge for multi-instance deployments.
- Do not log authorization headers, cookies, request bodies, refresh tokens, or dictionary words.
