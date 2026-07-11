namespace Cluely.Application.Auth.Logout;

public sealed record LogoutUserCommand(string RefreshToken, Guid CorrelationId);
