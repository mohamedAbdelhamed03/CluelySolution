namespace Cluely.IntegrationTests.Infrastructure;

public static class TestAuthConfiguration
{
    public const string Issuer = "Cluely";
    public const string Audience = "Cluely";
    public const string SigningKey = "Cluely-Development-Signing-Key-Minimum-32-Chars!";
    public const string DefaultPassword = "Password1!";

    public static Dictionary<string, string?> AsConfiguration() => new()
    {
        ["Jwt:Issuer"] = Issuer,
        ["Jwt:Audience"] = Audience,
        ["Jwt:SigningKey"] = SigningKey,
        ["Jwt:AccessTokenExpirationMinutes"] = "15"
    };
}
