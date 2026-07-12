using System.ComponentModel.DataAnnotations;

namespace Cluely.Api.Contracts.Requests;

public sealed class RegisterUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; init; } = string.Empty;
}

public sealed class LoginUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed class ExternalLoginRequest
{
    [Required]
    public string Provider { get; init; } = string.Empty;

    [Required]
    public string Token { get; init; } = string.Empty;
}

public sealed class LinkExternalLoginRequest
{
    [Required]
    public string Provider { get; init; } = string.Empty;

    [Required]
    public string Token { get; init; } = string.Empty;
}

public sealed class LogoutUserRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
