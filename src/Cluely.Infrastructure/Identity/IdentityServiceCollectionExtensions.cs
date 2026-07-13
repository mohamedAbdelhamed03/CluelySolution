using Cluely.Application.Common.Ports.Identity;
using Cluely.Infrastructure.Identity.ExternalAuth;
using Cluely.Infrastructure.Identity.ExternalAuth.Providers;
using Cluely.Infrastructure.Identity.Repositories;
using Cluely.Infrastructure.Identity.Security;
using Cluely.Infrastructure.Identity.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cluely.Infrastructure.Identity;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<ExternalAuthOptions>(configuration.GetSection(ExternalAuthOptions.SectionName));

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IExternalLoginRepository, ExternalLoginRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IParticipantBindingRepository, ParticipantBindingRepository>();
        services.AddScoped<IParticipantBindingResolver, ParticipantBindingResolver>();
        services.AddScoped<IAuthenticationSessionIssuer, AuthenticationSessionIssuer>();
        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        services.AddSingleton<IRefreshTokenHasher, Sha256RefreshTokenHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IRefreshTokenFactory, RefreshTokenFactory>();

        services.AddHttpClient<GoogleExternalIdentityProvider>();
        services.AddHttpClient<FacebookExternalIdentityProvider>();
        services.AddSingleton<AppleExternalIdentityProvider>();
        services.AddScoped<IExternalIdentityProviderRegistry>(serviceProvider =>
            new ExternalIdentityProviderRegistry(
            [
                serviceProvider.GetRequiredService<GoogleExternalIdentityProvider>(),
                serviceProvider.GetRequiredService<FacebookExternalIdentityProvider>(),
                serviceProvider.GetRequiredService<AppleExternalIdentityProvider>(),
            ]));

        return services;
    }
}
