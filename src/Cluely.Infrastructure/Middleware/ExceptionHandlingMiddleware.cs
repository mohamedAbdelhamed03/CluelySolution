using System.Text.Json;
using Cluely.Application.Common;
using Cluely.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Cluely.Infrastructure.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var statusCode = MapStatusCode(ex);
            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                logger.LogError(ex, "Unhandled server exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            }
            else
            {
                logger.LogWarning(
                    ex,
                    "Request rejected with status {StatusCode} for {Method} {Path}",
                    statusCode,
                    context.Request.Method,
                    context.Request.Path);
            }

            await WriteProblemDetailsAsync(context, ex, statusCode);
        }
    }

    private static int MapStatusCode(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            ParticipantBindingNotFoundException => StatusCodes.Status403Forbidden,
            DomainException => StatusCodes.Status400BadRequest,
            Cluely.Application.Common.ApplicationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static string MapErrorCode(Exception exception)
    {
        return exception switch
        {
            ParticipantBindingNotFoundException => ParticipantBindingNotFoundException.ErrorCode,
            UnauthorizedAccessException => "Unauthorized",
            DomainException domainException => domainException.GetType().Name,
            Cluely.Application.Common.ApplicationException applicationException => applicationException.GetType().Name,
            _ => "UnexpectedError"
        };
    }

    private static Task WriteProblemDetailsAsync(HttpContext context, Exception exception, int statusCode)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var correlationId = context.Items.TryGetValue(CorrelationIdConstants.ItemKey, out var value)
            ? value as string ?? Guid.NewGuid().ToString()
            : Guid.NewGuid().ToString();

        var problemDetails = new ProblemDetails
        {
            Title = statusCode switch
            {
                StatusCodes.Status401Unauthorized => "Unauthorized",
                StatusCodes.Status403Forbidden => "Forbidden",
                StatusCodes.Status400BadRequest => "Bad Request",
                _ => "An error occurred"
            },
            Status = statusCode,
            Type = "https://tools.ietf.org/html/rfc7807",
            Detail = statusCode >= StatusCodes.Status500InternalServerError
                ? "An unexpected server error occurred."
                : exception.Message,
            Instance = context.Request.Path,
            Extensions =
            {
                ["code"] = MapErrorCode(exception),
                ["correlationId"] = correlationId
            }
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
    }
}
