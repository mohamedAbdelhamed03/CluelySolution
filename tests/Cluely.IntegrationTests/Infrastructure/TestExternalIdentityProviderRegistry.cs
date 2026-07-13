using Cluely.Application.Common.Ports.Identity;

namespace Cluely.IntegrationTests.Infrastructure;

public sealed class TestExternalIdentityProvider : IExternalIdentityProvider
{
    public TestExternalIdentityProvider(string providerName)
    {
        ProviderName = providerName;
    }

    public string ProviderName { get; }

    public Dictionary<string, ExternalTokenValidationResult> TokenResults { get; } = new(StringComparer.Ordinal);

    public ExternalTokenValidationResult DefaultResult { get; set; } = new(
        false,
        null,
        ExternalTokenValidationFailureReason.InvalidToken);

    public Task<ExternalTokenValidationResult> ValidateTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (TokenResults.TryGetValue(token, out var result))
        {
            return Task.FromResult(result);
        }

        return Task.FromResult(DefaultResult);
    }
}

public sealed class TestExternalIdentityProviderRegistry : IExternalIdentityProviderRegistry
{
    private readonly Dictionary<string, TestExternalIdentityProvider> _providers = new(StringComparer.OrdinalIgnoreCase);

    public TestExternalIdentityProvider Google => GetOrCreate(ExternalAuthProviders.Google);

    public TestExternalIdentityProvider Facebook => GetOrCreate(ExternalAuthProviders.Facebook);

    public TestExternalIdentityProvider Apple => GetOrCreate(ExternalAuthProviders.Apple);

    public IExternalIdentityProvider? Resolve(string providerName)
        => _providers.TryGetValue(providerName, out var provider) ? provider : null;

    private TestExternalIdentityProvider GetOrCreate(string providerName)
    {
        if (!_providers.TryGetValue(providerName, out var provider))
        {
            provider = new TestExternalIdentityProvider(providerName);
            _providers[providerName] = provider;
        }

        return provider;
    }
}
