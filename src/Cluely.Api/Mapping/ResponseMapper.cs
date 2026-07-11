using Cluely.Api.Contracts.Responses;
using Cluely.Application.Common.ReadModels;
using Cluely.Application.Queries.GetRoom;
using Cluely.Application.Queries.GetRoomParticipants;
using Cluely.Application.Queries.GetRoomProjection;
using Cluely.Application.Rooms.CreateRoom;
using Cluely.Application.Rooms.JoinRoom;

namespace Cluely.Api.Mapping;

public static class ResponseMapper
{
    public static CreateRoomResponse ToResponse(this CreateRoomResult result)
        => new(result.RoomId, result.RoomCode, result.HostParticipantId);

    public static JoinRoomResponse ToResponse(this JoinRoomResult result)
        => new(result.RoomId, result.ParticipantId);

    public static RoomSummaryResponse ToResponse(this GetRoomResult result)
        => new(
            result.Room.RoomId,
            result.Room.RoomCode,
            result.Room.State,
            result.Room.AggregateVersion);

    public static RoomProjectionResponse ToResponse(this GetRoomProjectionResult result)
        => ToResponse(result.Projection);

    public static ParticipantsResponse ToResponse(this GetRoomParticipantsResult result)
        => new(result.Participants.Select(ToResponse).ToList());

    private static RoomProjectionResponse ToResponse(RoomProjectionReadModel projection)
        => new(
            projection.RoomId,
            projection.RoomCode,
            projection.State,
            projection.AggregateVersion,
            projection.Participants.Select(ToResponse).ToList(),
            projection.Dictionary is null
                ? null
                : new DictionaryResponse(projection.Dictionary.RegionCode, projection.Dictionary.ContentVersion),
            projection.Board is null
                ? null
                : new BoardResponse(
                    projection.Board.Cards.Select(card => new CardResponse(
                        card.Position,
                        card.Word,
                        card.IsRevealed,
                        card.Ownership)).ToList(),
                    projection.Board.RedRemaining,
                    projection.Board.BlueRemaining),
            projection.CurrentTurn is null
                ? null
                : new TurnResponse(
                    projection.CurrentTurn.Number,
                    projection.CurrentTurn.ActiveTeam,
                    projection.CurrentTurn.ClueWord,
                    projection.CurrentTurn.ClueNumber,
                    projection.CurrentTurn.GuessesUsed),
            projection.WinningTeam,
            projection.StartingTeam);

    private static ParticipantResponse ToResponse(ParticipantReadModel participant)
        => new(
            participant.ParticipantId,
            participant.Nickname,
            participant.Team,
            participant.Role,
            participant.IsHost);
}
