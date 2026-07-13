namespace Cluely.Application.Auth.LinkExternalLogin;

public sealed record LinkExternalLoginCommand(
    Guid UserId,
    string Provider,
    string Token,
    Guid CorrelationId);
