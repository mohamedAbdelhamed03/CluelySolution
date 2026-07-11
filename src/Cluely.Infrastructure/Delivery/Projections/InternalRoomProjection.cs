namespace Cluely.Infrastructure.Delivery.Projections;

public sealed class InternalRoomProjection
{
    public Guid RoomId { get; init; }
    public string RoomCode { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public int AggregateVersion { get; init; }
    public List<InternalParticipantProjection> Participants { get; init; } = [];
    public InternalDictionaryProjection? Dictionary { get; init; }
    public InternalBoardProjection? Board { get; init; }
    public InternalTurnProjection? CurrentTurn { get; init; }
    public string? WinningTeam { get; init; }
    public string StartingTeam { get; init; } = string.Empty;
}

public sealed class InternalParticipantProjection
{
    public Guid ParticipantId { get; init; }
    public string Nickname { get; init; } = string.Empty;
    public string Team { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public bool IsHost { get; init; }
}

public sealed class InternalDictionaryProjection
{
    public string RegionCode { get; init; } = string.Empty;
    public string ContentVersion { get; init; } = string.Empty;
}

public sealed class InternalBoardProjection
{
    public List<InternalCardProjection> Cards { get; init; } = [];
    public int RedRemaining { get; init; }
    public int BlueRemaining { get; init; }
}

public sealed class InternalCardProjection
{
    public int Position { get; init; }
    public string Word { get; init; } = string.Empty;
    public bool IsRevealed { get; init; }
    public string Ownership { get; init; } = string.Empty;
}

public sealed class InternalTurnProjection
{
    public int Number { get; init; }
    public string ActiveTeam { get; init; } = string.Empty;
    public string? ClueWord { get; init; }
    public int? ClueNumber { get; init; }
    public int GuessesUsed { get; init; }
}
