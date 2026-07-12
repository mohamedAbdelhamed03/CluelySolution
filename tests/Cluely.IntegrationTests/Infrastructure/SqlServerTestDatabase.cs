using Cluely.Infrastructure.Persistence;
using Cluely.Infrastructure.Persistence.DictionaryStore;
using Cluely.Infrastructure.Persistence.RoomCustody;
using Cluely.Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cluely.IntegrationTests.Infrastructure;

public sealed class SqlServerTestDatabase : IAsyncLifetime
{
    private readonly string _databaseName = $"CluelyTest_{Guid.NewGuid():N}";
    private ServiceProvider? _serviceProvider;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        ConnectionString =
            $"Server=(localdb)\\mssqllocaldb;Database={_databaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true";

        var services = new ServiceCollection();
        services.AddDbContext<CluelyDbContext>(options => options.UseSqlServer(ConnectionString));
        _serviceProvider = services.BuildServiceProvider();

        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CluelyDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider is not null)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CluelyDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await _serviceProvider.DisposeAsync();
        }
    }

    public SqlRoomCustodyTestContext CreateCustodyContext()
    {
        if (_serviceProvider is null)
        {
            throw new InvalidOperationException("Test database has not been initialized.");
        }

        return new SqlRoomCustodyTestContext(_serviceProvider);
    }

    public DictionaryStoreTestContext CreateDictionaryContext()
    {
        if (_serviceProvider is null)
        {
            throw new InvalidOperationException("Test database has not been initialized.");
        }

        return new DictionaryStoreTestContext(_serviceProvider);
    }
}

public sealed class DictionaryStoreTestContext : IAsyncDisposable
{
    private readonly IServiceScope _scope;

    public DictionaryStoreTestContext(IServiceProvider serviceProvider)
    {
        _scope = serviceProvider.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<CluelyDbContext>();
        Repository = new SqlDictionaryRepository(DbContext);
        ReadModel = new DictionaryReadModelProvider(DbContext);
    }

    public CluelyDbContext DbContext { get; }

    public SqlDictionaryRepository Repository { get; }

    public DictionaryReadModelProvider ReadModel { get; }

    public ValueTask DisposeAsync()
    {
        _scope.Dispose();
        return ValueTask.CompletedTask;
    }
}

public sealed class SqlRoomCustodyTestContext : IAsyncDisposable
{
    private readonly IServiceScope _scope;

    public SqlRoomCustodyTestContext(IServiceProvider serviceProvider)
    {
        _scope = serviceProvider.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<CluelyDbContext>();
        Custody = new SqlRoomCustody(DbContext);
    }

    public CluelyDbContext DbContext { get; }

    public SqlRoomCustody Custody { get; }

    public ValueTask DisposeAsync()
    {
        _scope.Dispose();
        return ValueTask.CompletedTask;
    }
}
