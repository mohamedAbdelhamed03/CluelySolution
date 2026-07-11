using Cluely.Application.Common.Ports.Identity;
using Microsoft.AspNetCore.Identity;

namespace Cluely.Infrastructure.Identity.Security;

public sealed class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<PasswordHasherUser> _passwordHasher = new();

    public string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(new PasswordHasherUser(), password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return _passwordHasher.VerifyHashedPassword(new PasswordHasherUser(), passwordHash, password)
            != PasswordVerificationResult.Failed;
    }

    private sealed class PasswordHasherUser;
}
