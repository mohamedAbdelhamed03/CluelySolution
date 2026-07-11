using System.Security.Cryptography;
using System.Text;
using Cluely.Application.Common.Ports.Identity;

namespace Cluely.Infrastructure.Identity.Security;

public sealed class Sha256RefreshTokenHasher : IRefreshTokenHasher
{
    public string Hash(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }
}
