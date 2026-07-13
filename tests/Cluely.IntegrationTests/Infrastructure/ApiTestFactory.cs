using Cluely.Application.Common.Ports.Identity;
using Cluely.Infrastructure.Identity;
using Cluely.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cluely.IntegrationTests.Infrastructure;

public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    public TestExternalIdentityProviderRegistry ExternalProviders { get; } = new();

    public ApiTestFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.Sources.Clear();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CluelyDb"] = _connectionString,
                ["Jwt:Issuer"] = TestAuthConfiguration.Issuer,
                ["Jwt:Audience"] = TestAuthConfiguration.Audience,
                ["Jwt:SigningKey"] = TestAuthConfiguration.SigningKey,
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Cors:AllowedOrigins:0"] = "https://frontend.test"
            });
        });

        builder.ConfigureServices(services =>
        {
            ReplaceDbContext<CluelyDbContext>(services);
            ReplaceDbContext<IdentityDbContext>(services);

            var moderatorDescriptor = services.SingleOrDefault(
                service => service.ServiceType == typeof(IContentModeratorAccessor));
            if (moderatorDescriptor is not null)
            {
                services.Remove(moderatorDescriptor);
            }

            services.AddScoped<IContentModeratorAccessor, TestContentModeratorAccessor>();

            var providerRegistryDescriptor = services.SingleOrDefault(
                service => service.ServiceType == typeof(IExternalIdentityProviderRegistry));
            if (providerRegistryDescriptor is not null)
            {
                services.Remove(providerRegistryDescriptor);
            }

            services.AddSingleton(ExternalProviders);
            services.AddSingleton<IExternalIdentityProviderRegistry>(sp => sp.GetRequiredService<TestExternalIdentityProviderRegistry>());
        });
    }

    private void ReplaceDbContext<TContext>(IServiceCollection services)
        where TContext : DbContext
    {
        var descriptor = services.SingleOrDefault(
            service => service.ServiceType == typeof(DbContextOptions<TContext>));
        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }

        services.AddDbContext<TContext>(options => options.UseSqlServer(_connectionString));
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var roomDbContext = scope.ServiceProvider.GetRequiredService<CluelyDbContext>();
        var identityDbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await roomDbContext.Database.MigrateAsync();
        await identityDbContext.Database.MigrateAsync();
    }
}
