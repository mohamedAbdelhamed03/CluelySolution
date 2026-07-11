namespace Cluely.Application.Auth.Register;

public sealed record RegisterUserCommand(string Email, string Password, Guid CorrelationId);
