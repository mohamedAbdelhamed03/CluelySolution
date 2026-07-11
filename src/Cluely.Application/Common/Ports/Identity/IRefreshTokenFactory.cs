namespace Cluely.Application.Common.Ports.Identity;

public sealed record CreatedRefreshToken(string PlainTextToken, RefreshTokenRecord Record);

public interface IRefreshTokenFactory
{
    CreatedRefreshToken Create(Guid userId);
}
