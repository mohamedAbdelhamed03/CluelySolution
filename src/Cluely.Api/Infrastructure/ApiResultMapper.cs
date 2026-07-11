using Cluely.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace Cluely.Api.Infrastructure;

public static class ApiResultMapper
{
    public static IActionResult ToActionResult(this Result result, ControllerBase controller)
    {
        if (result.IsSuccess)
        {
            return controller.NoContent();
        }

        return ToFailureResult(result.Error!, controller);
    }

    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        ControllerBase controller,
        Func<T, object?>? successMapper = null,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            var body = successMapper is null ? result.Value : successMapper(result.Value);
            return controller.StatusCode(successStatusCode, body);
        }

        return ToFailureResult(result.Error!, controller);
    }

    private static IActionResult ToFailureResult(Error error, ControllerBase controller)
    {
        var statusCode = MapStatusCode(error);
        var problemDetails = CreateProblemDetails(error, controller, statusCode);

        if (error is ValidationError validationError)
        {
            var validationProblem = new ValidationProblemDetails(validationError.Errors)
            {
                Title = validationError.Message,
                Status = statusCode,
                Type = "https://tools.ietf.org/html/rfc7807",
                Detail = validationError.Message,
                Instance = controller.HttpContext.Request.Path
            };
            validationProblem.Extensions["code"] = validationError.Code;
            validationProblem.Extensions["correlationId"] = GetCorrelationId(controller);
            return new ObjectResult(validationProblem)
            {
                StatusCode = statusCode,
                ContentTypes = { "application/problem+json" }
            };
        }

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }

    private static ProblemDetails CreateProblemDetails(Error error, ControllerBase controller, int statusCode)
    {
        return new ProblemDetails
        {
            Title = error.Message,
            Status = statusCode,
            Type = "https://tools.ietf.org/html/rfc7807",
            Detail = error.Message,
            Instance = controller.HttpContext.Request.Path,
            Extensions =
            {
                ["code"] = error.Code,
                ["correlationId"] = GetCorrelationId(controller)
            }
        };
    }

    private static int MapStatusCode(Error error)
    {
        return error switch
        {
            ValidationError => StatusCodes.Status400BadRequest,
            BusinessError { Code: "RoomNotFound" } => StatusCodes.Status404NotFound,
            BusinessError { Code: "ParticipantNotFound" } => StatusCodes.Status404NotFound,
            BusinessError { Code: "NotAMemberException" } => StatusCodes.Status404NotFound,
            BusinessError { Code: "UserNotFound" } => StatusCodes.Status404NotFound,
            BusinessError { Code: "InvalidCredentials" } => StatusCodes.Status401Unauthorized,
            BusinessError { Code: "InvalidRefreshToken" } => StatusCodes.Status401Unauthorized,
            BusinessError { Code: "DuplicateEmail" } => StatusCodes.Status409Conflict,
            BusinessError => StatusCodes.Status409Conflict,
            UnexpectedError => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static string GetCorrelationId(ControllerBase controller)
    {
        return controller.HttpContext.Items[CorrelationId.ItemKey] as string ?? Guid.NewGuid().ToString();
    }
}
