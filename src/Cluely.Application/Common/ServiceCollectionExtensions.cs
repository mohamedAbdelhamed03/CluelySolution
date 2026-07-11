using Cluely.Application.Gameplay.EndTurn;
using Cluely.Application.Gameplay.StartMatch;
using Cluely.Application.Gameplay.SubmitClue;
using Cluely.Application.Gameplay.SubmitGuess;
using Cluely.Application.Rooms.AssignRole;
using Cluely.Application.Rooms.AssignTeam;
using Cluely.Application.Rooms.CreateRoom;
using Cluely.Application.Rooms.JoinRoom;
using Cluely.Application.Rooms.LeaveRoom;
using Cluely.Application.Rooms.SelectDictionary;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cluely.Application.Common;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ServiceCollectionExtensions).Assembly);

        // Rooms Handlers
        services.AddTransient<CreateRoomHandler>();
        services.AddTransient<JoinRoomHandler>();
        services.AddTransient<LeaveRoomHandler>();
        services.AddTransient<AssignTeamHandler>();
        services.AddTransient<AssignRoleHandler>();
        services.AddTransient<SelectDictionaryHandler>();

        // Gameplay Handlers
        services.AddTransient<StartMatchHandler>();
        services.AddTransient<SubmitClueHandler>();
        services.AddTransient<SubmitGuessHandler>();
        services.AddTransient<EndTurnHandler>();

        return services;
    }
}
