# Environments and Configuration

ASP.NET Core environment variables use `__` as the section separator.

## Required production values

| Variable | Purpose |
|----------|---------|
| `ASPNETCORE_ENVIRONMENT=Production` | Enables production middleware behavior and HSTS. |
| `ConnectionStrings__CluelyDb` | SQL connection used by primary/content and identity DbContexts. |
| `Jwt__Issuer` | Expected JWT issuer. |
| `Jwt__Audience` | Expected JWT audience. |
| `Jwt__SigningKey` | Symmetric signing secret (minimum 32 random bytes; secret-manager supplied). |

## Operational values

| Variable | Default | Purpose |
|----------|---------|---------|
| `Jwt__AccessTokenExpirationMinutes` | `15` | Access-token lifetime. |
| `Cors__AllowedOrigins__0` | none | Exact trusted frontend origin; add indexed entries for more origins. |
| `ContentModeration__ModeratorUserIds__0` | none | User ID authorized for moderation commands. |
| `RequestLimits__MaxBodyBytes` | `1048576` | Kestrel request-body maximum. |
| `ExternalAuth__Google__ClientId` | none | Google OAuth client ID for ID token audience validation. |
| `ExternalAuth__Facebook__AppId` | none | Facebook application ID. |
| `ExternalAuth__Facebook__AppSecret` | none | Facebook application secret for token validation. |
| `ExternalAuth__Apple__ClientId` | none | Apple Services ID for identity token audience validation. |
| `AllowedHosts` | `*` | Host filtering; restrict to deployment hostnames in production. |

The checked-in signing key is development-only. Production secrets must come from environment variables or a secret provider and must not be committed.

## Security assumptions

- TLS is mandatory for bearer and refresh tokens.
- CORS is deny-by-default until exact origins are configured.
- Moderator authorization is deployment-controlled; an empty allow-list denies every moderation operation.
- The in-process rate limiter is per API instance. A multi-instance deployment must enforce distributed limits at the edge.
