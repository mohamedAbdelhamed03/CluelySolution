namespace Cluely.Application.Common.Ports.Identity;

public interface IExternalIdentityProviderRegistry
{
    IExternalIdentityProvider? Resolve(string providerName);
}
