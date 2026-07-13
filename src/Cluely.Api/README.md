# Cluely API

Transport-only REST surface for the Cluely backend.

## Principles

- Controllers map HTTP requests to Application commands/queries.
- Business rules live in the Domain; orchestration lives in Application.
- Errors are returned as RFC 7807 Problem Details via centralized `ApiResultMapper`.
- Real-time updates are delivered over SignalR at `/hubs/game`.

## Base URL

- Development HTTP: `http://localhost:5240`
- Development HTTPS: `https://localhost:7025`

## Versioning

MVP exposes a single version at `/api/*`. Future versions can be introduced as `/api/v2/*` without breaking existing clients.

## Correlation

Send `X-Correlation-Id` on any request. If omitted, the API generates one and returns it on the response.

## Authentication

Protected endpoints require a JWT bearer token.

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/auth/register` | Anonymous | Create account |
| POST | `/api/auth/login` | Anonymous | Obtain access + refresh tokens |
| POST | `/api/auth/external` | Anonymous | Social login via Google, Facebook, or Apple |
| POST | `/api/auth/external/link` | Bearer | Link an external provider to the current account |
| DELETE | `/api/auth/external/{provider}` | Bearer | Unlink an external provider |
| POST | `/api/auth/refresh` | Anonymous | Rotate refresh token |
| POST | `/api/auth/logout` | Anonymous | Revoke refresh token |
| GET | `/api/auth/me` | Bearer | Current user profile |

Send `Authorization: Bearer {accessToken}` on protected REST calls.

Swagger (Development) includes a Bearer security scheme for interactive testing.

### Participant binding

Identity is separate from room participation. After create/join, the API stores a `(userId, roomId) → participantId` binding. Gameplay and projection endpoints resolve the participant server-side; clients must not supply participant IDs for authorization.

## Endpoint Groups

### Health

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/health` | Anonymous | Service health check |

### Rooms (commands)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/rooms` | Bearer | Create room |
| POST | `/api/rooms/{roomCode}/join` | Bearer | Join by room code |
| POST | `/api/rooms/{roomId}/leave` | Bearer | Leave room |
| PATCH | `/api/rooms/{roomId}/team` | Bearer | Assign team |
| PATCH | `/api/rooms/{roomId}/role` | Bearer | Assign role |
| POST | `/api/rooms/{roomId}/dictionary` | Bearer | Select dictionary |

### Gameplay (commands)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/rooms/{roomId}/start` | Bearer | Start match |
| POST | `/api/rooms/{roomId}/clue` | Bearer | Submit clue |
| POST | `/api/rooms/{roomId}/guess` | Bearer | Submit guess |
| POST | `/api/rooms/{roomId}/end-turn` | Bearer | End turn |

### Projections (queries)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/rooms/{roomId}` | Bearer | Room summary |
| GET | `/api/rooms/{roomId}/projection` | Bearer | Role-filtered projection |
| GET | `/api/rooms/{roomId}/participants` | Bearer | Participant list |

### Content

All content endpoints require a bearer token.

| Capability | Endpoints |
|------------|-----------|
| Lifecycle | `POST /api/content`, `PATCH/DELETE /api/content/{id}`, `POST /api/content/{id}/restore` |
| Authoring | `POST/DELETE/PATCH /api/content/{id}/words`, `POST /api/content/{id}/validate` |
| Publishing | `POST /api/content/{id}/publish` |
| Discovery | `GET /api/content/mine`, `GET /api/content/discover`, `GET /api/content/{id}`, `GET /api/content/{id}/versions` |
| Sharing | `POST /api/content/{id}/share`, `DELETE /api/content/{id}/share/{userId}`, `POST /api/content/{id}/clone` |
| Moderation | `POST /submit-review`, `/approve`, `/reject`, `/block`, `/unblock`, and `/retire` under `/api/content/{id}` |

### Idempotency

- `POST /api/content/{id}/publish` requires an `Idempotency-Key` header containing a UUID.
- Repeating publish with the same key and dictionary returns the original version and does not create a duplicate.
- Create and clone accept the same header optionally; clients should send and retain a key whenever a request may be retried.
- Reusing a publish key for another dictionary returns `409 IdempotencyKeyConflict`.

```http
POST /api/content/6d67d4fd-b400-4e54-9d88-2a8d324590b6/publish HTTP/1.1
Authorization: Bearer {accessToken}
Idempotency-Key: 7e7d0195-1d35-4ec8-b6d5-8d79fb77b509
```

## Error Mapping

| Failure | HTTP Status |
|---------|-------------|
| Validation | 400 |
| Invalid credentials / refresh token | 401 |
| Missing participant binding | 403 |
| Room / participant not found | 404 |
| Business rule conflict / duplicate email | 409 |
| Unexpected | 500 |

All error responses include `code` and `correlationId` extensions in ProblemDetails.

## Security Notes (Production)

- Store `Jwt:SigningKey` in a secret manager, not source control.
- Terminate TLS at the edge; require HTTPS for token transmission.
- Configure `Cors:AllowedOrigins` with exact frontend origins; the default empty list denies cross-origin requests.
- Configure moderator user IDs through `ContentModeration:ModeratorUserIds`.
- The API applies global and authentication-specific rate limits. The edge proxy should add distributed limits for multi-instance deployments.
- Request bodies are limited to 1 MiB by Kestrel by default; enforce the same or a lower limit at the reverse proxy.

## OpenAPI

Swagger UI is available in Development at `/swagger`.

Every successful build writes the frontend-consumable OpenAPI 3.0 document to `src/Cluely.Api/openapi.json`. The artifact includes bearer requirements per protected operation, ProblemDetails responses, XML summaries, and idempotency header requirements.

## SignalR

Connect to `/hubs/game` with a valid JWT (pass `access_token` as a query parameter or use the Authorization header). Invoke `JoinRoom(roomId)`; the server resolves the participant from the authenticated user's room binding.
