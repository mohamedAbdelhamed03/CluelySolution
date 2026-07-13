using Cluely.Application.Common.Ports.Identity;

namespace Cluely.Infrastructure.Identity.ExternalAuth;

public sealed class ExternalIdentityProviderRegistry : IExternalIdentityProviderRegistry
{
    private readonly IReadOnlyDictionary<string, IExternalIdentityProvider> _providers;

    public ExternalIdentityProviderRegistry(IEnumerable<IExternalIdentityProvider> providers)
    {
        _providers = providers.ToDictionary(
            provider => provider.ProviderName,
            StringComparer.OrdinalIgnoreCase);
    }

    public IExternalIdentityProvider? Resolve(string providerName)
        => _providers.TryGetValue(providerName, out var provider) ? provider : null;
}
