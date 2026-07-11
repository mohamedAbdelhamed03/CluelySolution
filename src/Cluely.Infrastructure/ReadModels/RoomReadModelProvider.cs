using Cluely.Application.Common.Ports;
using Cluely.Application.Common.ReadModels;
using Cluely.Domain.Room.ValueObjects;
using Cluely.Infrastructure.Delivery.Projections;
using Cluely.Infrastructure.Delivery.Visibility;

namespace Cluely.Infrastructure.ReadModels;

public sealed class RoomReadModelProvider : IRoomReadModelProvider
{
    private readonly IRoomCustody _roomCustody;
    private readonly IProjectionBuilder _projectionBuilder;
    private readonly IVisibilityFilter _visibilityFilter;

    public RoomReadModelProvider(
        IRoomCustody roomCustody,
        IProjectionBuilder projectionBuilder,
        IVisibilityFilter visibilityFilter)
    {
        _roomCustody = roomCustody;
        _projectionBuilder = projectionBuilder;
        _visibilityFilter = visibilityFilter;
    }

    public async Task<RoomSummaryReadModel?> GetRoomSummaryAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default)
    {
        var room = await _roomCustody.GetAsync(roomId, cancellationToken);
        if (room is null)
        {
            return null;
        }

        return new RoomSummaryReadModel(
            room.Id.Value,
            room.Code.Value,
            room.State.ToString(),
            room.Version.Value);
    }

    public async Task<(RoomProjectionReadModel? Projection, string? FailureCode)> GetRoleFilteredProjectionAsync(
        RoomId roomId,
        ParticipantId participantId,
        CancellationToken cancellationToken = default)
    {
        var room = await _roomCustody.GetAsync(roomId, cancellationToken);
        if (room is null)
        {
            return (null, "RoomNotFound");
        }

        var participant = room.Participants.SingleOrDefault(p => p.Id == participantId);
        if (participant is null)
        {
            return (null, "ParticipantNotFound");
        }

        var internalProjection = _projectionBuilder.Build(room);
        var filtered = _visibilityFilter.Filter(internalProjection, participant.Role, participant.Team);

        return (new RoomProjectionReadModel(
            room.Id.Value,
            filtered.RoomCode,
            filtered.State,
            filtered.AggregateVersion,
            filtered.Participants.Select(MapParticipant).ToList(),
            filtered.Dictionary is null
                ? null
                : new DictionaryReadModel(filtered.Dictionary.RegionCode, filtered.Dictionary.ContentVersion),
            filtered.Board is null
                ? null
                : new BoardReadModel(
                    filtered.Board.Cards.Select(card => new CardReadModel(
                        card.Position,
                        card.Word,
                        card.IsRevealed,
                        card.Ownership)).ToList(),
                    filtered.Board.RedRemaining,
                    filtered.Board.BlueRemaining),
            filtered.CurrentTurn is null
                ? null
                : new TurnReadModel(
                    filtered.CurrentTurn.Number,
                    filtered.CurrentTurn.ActiveTeam,
                    filtered.CurrentTurn.ClueWord,
                    filtered.CurrentTurn.ClueNumber,
                    filtered.CurrentTurn.GuessesUsed),
            filtered.WinningTeam,
            filtered.StartingTeam), null);
    }

    public async Task<IReadOnlyList<ParticipantReadModel>?> GetParticipantsAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default)
    {
        var room = await _roomCustody.GetAsync(roomId, cancellationToken);
        if (room is null)
        {
            return null;
        }

        return room.Participants
            .Select(p => new ParticipantReadModel(
                p.Id.Value,
                p.Nickname,
                p.Team.Value,
                p.Role.Value,
                p.IsHost))
            .ToList();
    }

    private static ParticipantReadModel MapParticipant(Delivery.Contracts.ParticipantProjectionDto participant)
    {
        return new ParticipantReadModel(
            participant.ParticipantId,
            participant.Nickname,
            participant.Team,
            participant.Role,
            participant.IsHost);
    }
}
