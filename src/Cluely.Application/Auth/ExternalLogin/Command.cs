namespace Cluely.Application.Auth.ExternalLogin;

public sealed record ExternalLoginCommand(string Provider, string Token, Guid CorrelationId);
