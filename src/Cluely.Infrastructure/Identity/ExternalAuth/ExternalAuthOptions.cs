namespace Cluely.Infrastructure.Identity.ExternalAuth;

public sealed class ExternalAuthOptions
{
    public const string SectionName = "ExternalAuth";

    public GoogleAuthOptions Google { get; init; } = new();
    public FacebookAuthOptions Facebook { get; init; } = new();
    public AppleAuthOptions Apple { get; init; } = new();
}

public sealed class GoogleAuthOptions
{
    public string ClientId { get; init; } = string.Empty;
}

public sealed class FacebookAuthOptions
{
    public string AppId { get; init; } = string.Empty;
    public string AppSecret { get; init; } = string.Empty;
}

public sealed class AppleAuthOptions
{
    public string ClientId { get; init; } = string.Empty;
}
