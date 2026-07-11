using Cluely.Api.Contracts.Requests;
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

namespace Cluely.Api.Mapping;

public static class CommandMapper
{
    public static CreateRoomCommand ToCommand(this CreateRoomRequest request, Guid correlationId)
        => new(request.HostNickname, correlationId);

    public static JoinRoomCommand ToCommand(this JoinRoomRequest request, string roomCode, Guid correlationId)
        => new(roomCode, request.Nickname, correlationId);

    public static LeaveRoomCommand ToCommand(this LeaveRoomRequest request, Guid roomId, Guid correlationId)
        => new(roomId, request.ParticipantId, correlationId);

    public static AssignTeamCommand ToCommand(this AssignTeamRequest request, Guid roomId, Guid correlationId)
        => new(roomId, request.ParticipantId, request.Team, correlationId);

    public static AssignRoleCommand ToCommand(this AssignRoleRequest request, Guid roomId, Guid correlationId)
        => new(roomId, request.ParticipantId, request.Role, correlationId);

    public static SelectDictionaryCommand ToCommand(this SelectDictionaryRequest request, Guid roomId, Guid correlationId)
        => new(roomId, request.ParticipantId, request.RegionCode, request.ContentVersion, correlationId);

    public static StartMatchCommand ToCommand(this StartMatchRequest request, Guid roomId, Guid correlationId)
        => new(roomId, request.ParticipantId, correlationId);

    public static SubmitClueCommand ToCommand(this SubmitClueRequest request, Guid roomId, Guid correlationId)
        => new(roomId, request.ParticipantId, request.Word, request.Count, correlationId);

    public static SubmitGuessCommand ToCommand(this SubmitGuessRequest request, Guid roomId, Guid correlationId)
        => new(roomId, request.ParticipantId, request.CardPosition, correlationId);

    public static EndTurnCommand ToCommand(this EndTurnRequest request, Guid roomId, Guid correlationId)
        => new(roomId, request.ParticipantId, correlationId);
}
