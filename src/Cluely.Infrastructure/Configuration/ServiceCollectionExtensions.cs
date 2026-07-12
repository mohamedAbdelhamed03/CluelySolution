using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Ports.Content;
using Cluely.Application.Common.Ports.Identity;
using Cluely.Infrastructure.Content;
using Cluely.Infrastructure.Identity;
using Cluely.Infrastructure.ReadModels;
using Cluely.Infrastructure.Common;
using Cluely.Infrastructure.Delivery;
using Cluely.Infrastructure.Delivery.Connections;
using Cluely.Infrastructure.Delivery.Dispatch;
using Cluely.Infrastructure.Delivery.Projections;
using Cluely.Infrastructure.Delivery.Visibility;
using Cluely.Infrastructure.Persistence;
using Cluely.Infrastructure.Persistence.RoomCustody;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cluely.Infrastructure.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration? configuration = null)
    {
        services.AddDbContext<CluelyDbContext>(options =>
            options.UseSqlServer(connectionString));

        if (configuration is not null)
        {
            services.AddIdentityInfrastructure(configuration, connectionString);
        }

        services.AddScoped<IRoomCustody, SqlRoomCustody>();
        services.AddScoped<IRoomReadModelProvider, RoomReadModelProvider>();
        services.AddScoped<IDictionaryRepository, UnavailableDictionaryRepository>();
        services.AddScoped<IContentModeratorAccessor, UnavailableContentModeratorAccessor>();

        services.AddSingleton<IConnectionRegistry, ConnectionRegistry>();
        services.AddSingleton<IProjectionBuilder, ProjectionBuilder>();
        services.AddSingleton<IVisibilityFilter, VisibilityFilter>();

        services.AddScoped<IDeliveryDispatcher, SignalRDeliveryDispatcher>();
        services.AddScoped<IDomainEventPublisher, SignalRDomainEventPublisher>();
        services.AddScoped<IGameConnectionService, GameConnectionService>();

        services.AddSingleton<IGuidGenerator, GuidGenerator>();
        services.AddSingleton<IRoomCodeGenerator, RoomCodeGenerator>();

        return services;
    }

    public static IServiceCollection AddSignalRDelivery(this IServiceCollection services)
    {
        services.AddSignalR();
        return services;
    }
}
