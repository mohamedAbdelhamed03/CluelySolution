namespace Cluely.Application.Auth.UnlinkExternalLogin;

public sealed record UnlinkExternalLoginCommand(Guid UserId, string Provider, Guid CorrelationId);
