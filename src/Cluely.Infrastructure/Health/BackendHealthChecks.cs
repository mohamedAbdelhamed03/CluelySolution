using Cluely.Infrastructure.Delivery.Dispatch;
using Cluely.Infrastructure.Identity;
using Cluely.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cluely.Infrastructure.Health;

public sealed class PrimaryDatabaseHealthCheck(CluelyDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy("Primary SQL database is reachable.")
            : HealthCheckResult.Unhealthy("Primary SQL database is unreachable.");
    }
}

public sealed class IdentityDatabaseHealthCheck(IdentityDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy("Identity database is reachable.")
            : HealthCheckResult.Unhealthy("Identity database is unreachable.");
    }
}

public sealed class ContentPersistenceHealthCheck(CluelyDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.DictionarySnapshots
                .AsNoTracking()
                .Select(snapshot => snapshot.DictionaryId)
                .Take(1)
                .ToListAsync(cancellationToken);

            return HealthCheckResult.Healthy("Content persistence is queryable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Content persistence query failed.", exception);
        }
    }
}

public sealed class SignalRDeliveryHealthCheck(
    SignalRDomainEventPublisher roomPublisher,
    IDeliveryDispatcher dispatcher) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        _ = roomPublisher;
        _ = dispatcher;
        return Task.FromResult(HealthCheckResult.Healthy("SignalR delivery dependencies are registered."));
    }
}
