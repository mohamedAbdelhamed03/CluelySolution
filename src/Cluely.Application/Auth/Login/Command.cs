namespace Cluely.Application.Auth.Login;

public sealed record LoginUserCommand(string Email, string Password, Guid CorrelationId);
