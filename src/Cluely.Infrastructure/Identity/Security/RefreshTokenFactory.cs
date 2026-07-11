using System.Security.Cryptography;
using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Ports.Identity;

namespace Cluely.Infrastructure.Identity.Security;

public sealed class RefreshTokenFactory : IRefreshTokenFactory
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly TimeProvider _timeProvider;

    public RefreshTokenFactory(
        IGuidGenerator guidGenerator,
        IRefreshTokenHasher refreshTokenHasher,
        TimeProvider timeProvider)
    {
        _guidGenerator = guidGenerator;
        _refreshTokenHasher = refreshTokenHasher;
        _timeProvider = timeProvider;
    }

    public CreatedRefreshToken Create(Guid userId)
    {
        var plainTextToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var record = new RefreshTokenRecord(
            _guidGenerator.Generate(),
            userId,
            _refreshTokenHasher.Hash(plainTextToken),
            now.AddDays(7),
            now,
            RevokedAt: null,
            ReplacedByTokenHash: null);

        return new CreatedRefreshToken(plainTextToken, record);
    }
}
