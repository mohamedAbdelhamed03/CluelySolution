using Cluely.Application.Auth.GetCurrentUser;
using Cluely.Application.Auth.Login;
using Cluely.Application.Auth.Logout;
using Cluely.Application.Auth.Refresh;
using Cluely.Application.Auth.Register;
using Cluely.Application.Gameplay.EndTurn;
using Cluely.Application.Gameplay.StartMatch;
using Cluely.Application.Gameplay.SubmitClue;
using Cluely.Application.Gameplay.SubmitGuess;
using Cluely.Application.Queries.GetRoom;
using Cluely.Application.Queries.GetRoomParticipants;
using Cluely.Application.Queries.GetRoomProjection;
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

        services.AddTransient<CreateRoomHandler>();
        services.AddTransient<JoinRoomHandler>();
        services.AddTransient<LeaveRoomHandler>();
        services.AddTransient<AssignTeamHandler>();
        services.AddTransient<AssignRoleHandler>();
        services.AddTransient<SelectDictionaryHandler>();

        services.AddTransient<StartMatchHandler>();
        services.AddTransient<SubmitClueHandler>();
        services.AddTransient<SubmitGuessHandler>();
        services.AddTransient<EndTurnHandler>();

        services.AddTransient<GetRoomHandler>();
        services.AddTransient<GetRoomProjectionHandler>();
        services.AddTransient<GetRoomParticipantsHandler>();

        services.AddTransient<RegisterUserHandler>();
        services.AddTransient<LoginUserHandler>();
        services.AddTransient<RefreshTokenHandler>();
        services.AddTransient<LogoutUserHandler>();
        services.AddTransient<GetCurrentUserHandler>();

        return services;
    }
}
