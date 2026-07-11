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
- Apply login rate limiting at the API gateway or reverse proxy (not implemented in MVP — see technical debt TD-0502-05).

## OpenAPI

Swagger UI is available in Development at `/swagger`.

## SignalR

Connect to `/hubs/game` with a valid JWT (pass `access_token` as a query parameter or use the Authorization header). Invoke `JoinRoom(roomId)`; the server resolves the participant from the authenticated user's room binding.
