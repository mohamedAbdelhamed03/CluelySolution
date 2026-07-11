namespace Cluely.Application.Auth.Refresh;

public sealed record RefreshTokenCommand(string RefreshToken, Guid CorrelationId);
