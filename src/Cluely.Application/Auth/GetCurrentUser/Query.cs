namespace Cluely.Application.Auth.GetCurrentUser;

public sealed record GetCurrentUserQuery(Guid UserId, Guid CorrelationId);
