using Cluely.Api.Infrastructure;
using Cluely.Application.Common;
using Cluely.Infrastructure.Configuration;
using Cluely.Infrastructure.Delivery.Hubs;
using Cluely.Infrastructure.Identity;
using Cluely.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cluely.IntegrationTests.Infrastructure;

public sealed class SignalRTestHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private SignalRTestHost(WebApplication app)
    {
        _app = app;
    }

    public IServiceProvider Services => _app.Services;

    public TestServer Server => _app.GetTestServer();

    public static async Task<SignalRTestHost> CreateAsync(string connectionString)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();

        foreach (var setting in TestAuthConfiguration.AsConfiguration())
        {
            builder.Configuration[setting.Key] = setting.Value;
        }

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(connectionString, builder.Configuration);
        builder.Services.AddJwtAuthentication(builder.Configuration);
        builder.Services.AddSignalRDelivery();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHub<GameHub>("/hubs/game");
        await app.StartAsync();

        await using var scope = app.Services.CreateAsyncScope();
        var roomDbContext = scope.ServiceProvider.GetRequiredService<CluelyDbContext>();
        var identityDbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await roomDbContext.Database.MigrateAsync();
        await identityDbContext.Database.MigrateAsync();

        return new SignalRTestHost(app);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
