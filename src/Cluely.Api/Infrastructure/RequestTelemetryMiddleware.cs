using System.Diagnostics;
using System.Security.Claims;

namespace Cluely.Api.Infrastructure;

public sealed class RequestTelemetryMiddleware(
    RequestDelegate next,
    ILogger<RequestTelemetryMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var scope = new Dictionary<string, object?>();

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            scope["UserId"] = userId;
        }

        if (context.Request.RouteValues.TryGetValue("roomId", out var roomId))
        {
            scope["RoomId"] = roomId;
        }

        if (context.Request.Path.StartsWithSegments("/api/content")
            && context.Request.RouteValues.TryGetValue("id", out var dictionaryId))
        {
            scope["DictionaryId"] = dictionaryId;
        }

        using (logger.BeginScope(scope))
        {
            try
            {
                await next(context);
            }
            finally
            {
                stopwatch.Stop();
                logger.LogInformation(
                    "HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMilliseconds} ms.",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.Elapsed.TotalMilliseconds);
            }
        }
    }
}
