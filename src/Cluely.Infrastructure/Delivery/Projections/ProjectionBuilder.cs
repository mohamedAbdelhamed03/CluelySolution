using Cluely.Domain.Room;
using Cluely.Domain.Room.Entities;

namespace Cluely.Infrastructure.Delivery.Projections;

public sealed class ProjectionBuilder : IProjectionBuilder
{
    public InternalRoomProjection Build(Room room)
    {
        return new InternalRoomProjection
        {
            RoomId = room.Id.Value,
            RoomCode = room.Code.Value,
            State = room.State.ToString(),
            AggregateVersion = room.Version.Value,
            Participants = room.Participants.Select(p => new InternalParticipantProjection
            {
                ParticipantId = p.Id.Value,
                Nickname = p.Nickname,
                Team = p.Team.Value,
                Role = p.Role.Value,
                IsHost = p.IsHost,
            }).ToList(),
            Dictionary = room.Dictionary is null
                ? null
                : new InternalDictionaryProjection
                {
                    RegionCode = room.Dictionary.Region.Value,
                    ContentVersion = room.Dictionary.Version.Value,
                },
            Board = room.Board is null ? null : MapBoard(room.Board),
            CurrentTurn = room.CurrentTurn is null ? null : MapTurn(room.CurrentTurn),
            WinningTeam = room.WinningTeam?.Value,
            StartingTeam = room.StartingTeam.Value,
        };
    }

    private static InternalBoardProjection MapBoard(Board board)
    {
        return new InternalBoardProjection
        {
            Cards = board.Cards.Select(c => new InternalCardProjection
            {
                Position = c.Position.Value,
                Word = c.Word,
                IsRevealed = c.IsRevealed,
                Ownership = c.Ownership.Value,
            }).ToList(),
            RedRemaining = board.RedRemaining,
            BlueRemaining = board.BlueRemaining,
        };
    }

    private static InternalTurnProjection MapTurn(Turn turn)
    {
        return new InternalTurnProjection
        {
            Number = turn.Number,
            ActiveTeam = turn.ActiveTeam.Value,
            ClueWord = turn.Clue?.Word,
            ClueNumber = turn.Clue?.Number,
            GuessesUsed = turn.GuessesUsed,
        };
    }
}
