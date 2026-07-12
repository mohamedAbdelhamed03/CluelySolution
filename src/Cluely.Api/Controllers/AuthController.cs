using Cluely.Api.Contracts.Requests;
using Cluely.Api.Contracts.Responses;
using Cluely.Api.Infrastructure;
using Cluely.Api.Mapping;
using Cluely.Application.Auth.GetCurrentUser;
using Cluely.Application.Auth.Login;
using Cluely.Application.Auth.Logout;
using Cluely.Application.Auth.Refresh;
using Cluely.Application.Auth.Register;
using Cluely.Application.Common.Ports.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace Cluely.Api.Controllers;

/// <summary>
/// Authentication and identity endpoints.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[Tags("Auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly RegisterUserHandler _registerUserHandler;
    private readonly LoginUserHandler _loginUserHandler;
    private readonly RefreshTokenHandler _refreshTokenHandler;
    private readonly LogoutUserHandler _logoutUserHandler;
    private readonly GetCurrentUserHandler _getCurrentUserHandler;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public AuthController(
        RegisterUserHandler registerUserHandler,
        LoginUserHandler loginUserHandler,
        RefreshTokenHandler refreshTokenHandler,
        LogoutUserHandler logoutUserHandler,
        GetCurrentUserHandler getCurrentUserHandler,
        ICurrentUserAccessor currentUserAccessor)
    {
        _registerUserHandler = registerUserHandler;
        _loginUserHandler = loginUserHandler;
        _refreshTokenHandler = refreshTokenHandler;
        _logoutUserHandler = logoutUserHandler;
        _getCurrentUserHandler = getCurrentUserHandler;
        _currentUserAccessor = currentUserAccessor;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var correlationId = CorrelationIdAccessor.GetCorrelationId(HttpContext);
        var result = await _registerUserHandler.HandleAsync(
            new RegisterUserCommand(request.Email, request.Password, correlationId),
            cancellationToken);

        return result.ToActionResult(
            this,
            value => new RegisterUserResponse(value.UserId, value.Email),
            StatusCodes.Status201Created);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginUserRequest request, CancellationToken cancellationToken)
    {
        var correlationId = CorrelationIdAccessor.GetCorrelationId(HttpContext);
        var result = await _loginUserHandler.HandleAsync(
            new LoginUserCommand(request.Email, request.Password, correlationId),
            cancellationToken);

        return result.ToActionResult(this, value => new LoginUserResponse(
            value.UserId,
            value.Email,
            value.AccessToken,
            value.AccessTokenExpiresAt,
            value.RefreshToken,
            value.RefreshTokenExpiresAt));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var correlationId = CorrelationIdAccessor.GetCorrelationId(HttpContext);
        var result = await _refreshTokenHandler.HandleAsync(
            new RefreshTokenCommand(request.RefreshToken, correlationId),
            cancellationToken);

        return result.ToActionResult(this, value => new RefreshTokenResponse(
            value.AccessToken,
            value.AccessTokenExpiresAt,
            value.RefreshToken,
            value.RefreshTokenExpiresAt));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutUserRequest request, CancellationToken cancellationToken)
    {
        var correlationId = CorrelationIdAccessor.GetCorrelationId(HttpContext);
        var result = await _logoutUserHandler.HandleAsync(
            new LogoutUserCommand(request.RefreshToken, correlationId),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ToActionResult(this);
        }

        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = _currentUserAccessor.UserId
            ?? throw new UnauthorizedAccessException();

        var correlationId = CorrelationIdAccessor.GetCorrelationId(HttpContext);
        var result = await _getCurrentUserHandler.HandleAsync(
            new GetCurrentUserQuery(userId, correlationId),
            cancellationToken);

        return result.ToActionResult(this, value => new CurrentUserResponse(
            value.UserId,
            value.Email,
            value.AccountStatus));
    }
}
