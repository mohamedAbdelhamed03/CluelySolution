using Cluely.Domain.Room.ValueObjects;
using Cluely.Infrastructure.Delivery.Contracts;
using Cluely.Infrastructure.Delivery.Projections;

namespace Cluely.Infrastructure.Delivery.Visibility;

public sealed class VisibilityFilter : IVisibilityFilter
{
    public RoomProjectionDto Filter(InternalRoomProjection projection, Role role, Team team)
    {
        // Role determines board visibility per ADR-006. Team is reserved for future team-scoped projections.
        return new RoomProjectionDto(
            RoomCode: projection.RoomCode,
            State: projection.State,
            AggregateVersion: projection.AggregateVersion,
            Participants: projection.Participants.Select(p => new ParticipantProjectionDto(
                p.ParticipantId,
                p.Nickname,
                p.Team,
                p.Role,
                p.IsHost)).ToList(),
            Dictionary: projection.Dictionary is null
                ? null
                : new DictionaryProjectionDto(
                    projection.Dictionary.RegionCode,
                    projection.Dictionary.ContentVersion),
            Board: projection.Board is null
                ? null
                : FilterBoard(projection.Board, role),
            CurrentTurn: projection.CurrentTurn is null
                ? null
                : new TurnProjectionDto(
                    projection.CurrentTurn.Number,
                    projection.CurrentTurn.ActiveTeam,
                    projection.CurrentTurn.ClueWord,
                    projection.CurrentTurn.ClueNumber,
                    projection.CurrentTurn.GuessesUsed),
            WinningTeam: projection.WinningTeam,
            StartingTeam: projection.StartingTeam);
    }

    private static BoardProjectionDto FilterBoard(InternalBoardProjection board, Role role)
    {
        var isSpymaster = role == Role.Spymaster;

        return new BoardProjectionDto(
            Cards: board.Cards.Select(card => new CardProjectionDto(
                card.Position,
                card.Word,
                card.IsRevealed,
                Ownership: isSpymaster || card.IsRevealed ? card.Ownership : null)).ToList(),
            RedRemaining: board.RedRemaining,
            BlueRemaining: board.BlueRemaining);
    }
}
