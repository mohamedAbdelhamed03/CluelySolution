using Cluely.Application.Auth.GetCurrentUser;
using Cluely.Application.Auth.Login;
using Cluely.Application.Auth.Logout;
using Cluely.Application.Auth.Refresh;
using Cluely.Application.Auth.Register;
using Cluely.Application.Content.AddWord;
using Cluely.Application.Content.ApproveReview;
using Cluely.Application.Content.ArchiveDictionary;
using Cluely.Application.Content.BlockVersion;
using Cluely.Application.Content.BulkAddWords;
using Cluely.Application.Content.CancelDeleteDictionary;
using Cluely.Application.Content.CreateDictionary;
using Cluely.Application.Content.DeleteDictionary;
using Cluely.Application.Content.Discovery.GetDictionaryDetails;
using Cluely.Application.Content.Discovery.GetDictionaryVersions;
using Cluely.Application.Content.Discovery.GetDiscoverableDictionaries;
using Cluely.Application.Content.Discovery.GetMyDictionaries;
using Cluely.Application.Content.PublishDictionary;
using Cluely.Application.Content.RejectReview;
using Cluely.Application.Content.RemoveWord;
using Cluely.Application.Content.ReplaceWord;
using Cluely.Application.Content.ReportDictionary;
using Cluely.Application.Content.RestoreDictionary;
using Cluely.Application.Content.RenameDictionary;
using Cluely.Application.Content.RetireVersion;
using Cluely.Application.Content.SubmitForReview;
using Cluely.Application.Content.UnblockVersion;
using Cluely.Application.Content.ValidateDraft;
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

        services.AddTransient<CreateDictionaryHandler>();
        services.AddTransient<RenameDictionaryHandler>();
        services.AddTransient<ArchiveDictionaryHandler>();
        services.AddTransient<DeleteDictionaryHandler>();
        services.AddTransient<CancelDeleteDictionaryHandler>();
        services.AddTransient<RestoreDictionaryHandler>();

        services.AddTransient<AddWordHandler>();
        services.AddTransient<RemoveWordHandler>();
        services.AddTransient<ReplaceWordHandler>();
        services.AddTransient<BulkAddWordsHandler>();
        services.AddTransient<ValidateDraftHandler>();

        services.AddTransient<PublishDictionaryHandler>();

        services.AddTransient<ReportDictionaryHandler>();

        services.AddTransient<SubmitForReviewHandler>();
        services.AddTransient<ApproveReviewHandler>();
        services.AddTransient<RejectReviewHandler>();
        services.AddTransient<BlockVersionHandler>();
        services.AddTransient<UnblockVersionHandler>();
        services.AddTransient<RetireVersionHandler>();

        services.AddTransient<GetMyDictionariesHandler>();
        services.AddTransient<GetDiscoverableDictionariesHandler>();
        services.AddTransient<GetDictionaryDetailsHandler>();
        services.AddTransient<GetDictionaryVersionsHandler>();

        return services;
    }
}
