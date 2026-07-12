namespace Cluely.Api.Infrastructure;

/// <summary>
/// Reads the client-supplied <c>Idempotency-Key</c> header used to make create/clone replay
/// deterministic. When absent or malformed, a fresh key is generated (a non-retried request).
/// </summary>
public static class IdempotencyKeyAccessor
{
    public const string HeaderName = "Idempotency-Key";

    public static Guid GetIdempotencyKey(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue(HeaderName, out var value)
            && Guid.TryParse(value.ToString(), out var key))
        {
            return key;
        }

        return Guid.NewGuid();
    }
}
