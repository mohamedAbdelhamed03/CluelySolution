# External Authentication

Cluely supports social login through a unified external authentication endpoint. Provider-specific token validation happens server-side in Infrastructure; Application handlers depend only on `IExternalIdentityProvider` abstractions.

## Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/auth/external` | Anonymous | Validate provider token and issue JWT + refresh token |
| POST | `/api/auth/external/link` | Bearer | Link a provider to the authenticated account |
| DELETE | `/api/auth/external/{provider}` | Bearer | Unlink a provider from the authenticated account |

Supported providers: `google`, `facebook`, `apple`.

### External login request

```json
{
  "provider": "google",
  "token": "<provider token>"
}
```

Successful responses reuse the same `LoginUserResponse` contract as email/password login.

## Security model

- Provider tokens are validated server-side.
- Client-supplied profile data is never trusted.
- Users are created only from verified provider claims.
- Provider access tokens are not persisted.
- `ExternalLogins` stores provider name, provider user id, verified email metadata, and link timestamp.
- Provider identifiers are not exposed through public APIs.

## Account linking rules

- One internal user may link multiple providers.
- A provider account can be linked to only one internal user.
- A user cannot link the same provider twice.
- Unlinking is blocked when it would remove the last available login method.

## Configuration

`appsettings.json` section:

```json
"ExternalAuth": {
  "Google": { "ClientId": "<google-oauth-client-id>" },
  "Facebook": { "AppId": "<facebook-app-id>", "AppSecret": "<facebook-app-secret>" },
  "Apple": { "ClientId": "<apple-services-id>" }
}
```

Environment variables:

| Variable | Purpose |
|----------|---------|
| `ExternalAuth__Google__ClientId` | Google OAuth client ID used to validate ID token audience |
| `ExternalAuth__Facebook__AppId` | Facebook application ID |
| `ExternalAuth__Facebook__AppSecret` | Facebook application secret for debug_token validation |
| `ExternalAuth__Apple__ClientId` | Apple Services ID / bundle client ID |

## Provider setup

### Google

1. Create an OAuth 2.0 client in Google Cloud Console.
2. Configure authorized JavaScript origins and redirect URIs for the frontend.
3. Send the Google ID token to `POST /api/auth/external`.
4. Set `ExternalAuth__Google__ClientId` to the web client ID.

### Facebook

1. Create a Facebook app with Facebook Login enabled.
2. Configure valid OAuth redirect URIs for local and production frontends.
3. Send the user access token to `POST /api/auth/external`.
4. Set `ExternalAuth__Facebook__AppId` and `ExternalAuth__Facebook__AppSecret`.

### Apple

1. Create a Services ID in Apple Developer.
2. Configure Sign in with Apple and return URLs for local/production frontends.
3. Send the Apple identity token to `POST /api/auth/external`.
4. Set `ExternalAuth__Apple__ClientId` to the Services ID.

## Local development

- Leave provider secrets empty to keep providers unavailable outside tests.
- Integration tests replace `IExternalIdentityProviderRegistry` with deterministic test providers.
- Apply the Identity migration `AddExternalLogins` before exercising external login locally.

## Production

- Store all provider secrets in environment variables or a secret manager.
- Keep redirect URIs aligned with deployed frontend origins.
- External login endpoints use the same auth rate limiter as email/password login.
