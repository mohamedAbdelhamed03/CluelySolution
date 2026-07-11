namespace Cluely.Application.Common.Ports.Identity;

public interface IRefreshTokenHasher
{
    string Hash(string refreshToken);
}
