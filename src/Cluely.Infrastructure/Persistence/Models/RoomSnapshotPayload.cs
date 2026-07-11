namespace Cluely.Infrastructure.Persistence.Models;

internal sealed class RoomSnapshotPayload
{
    public int RoomState { get; init; }
    public string StartingTeam { get; init; } = string.Empty;
    public string? WinningTeam { get; init; }
    public string? DictionaryRegionCode { get; init; }
    public string? DictionaryContentVersion { get; init; }
    public List<ParticipantPayload> Participants { get; init; } = [];
    public BoardPayload? Board { get; init; }
    public TurnPayload? CurrentTurn { get; init; }
}

internal sealed class ParticipantPayload
{
    public Guid ParticipantId { get; init; }
    public string Nickname { get; init; } = string.Empty;
    public string Team { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public bool IsHost { get; init; }
}

internal sealed class BoardPayload
{
    public List<CardPayload> Cards { get; init; } = [];
    public int RedRemaining { get; init; }
    public int BlueRemaining { get; init; }
}

internal sealed class CardPayload
{
    public int Position { get; init; }
    public string Word { get; init; } = string.Empty;
    public string Ownership { get; init; } = string.Empty;
    public bool IsRevealed { get; init; }
}

internal sealed class TurnPayload
{
    public int Number { get; init; }
    public string ActiveTeam { get; init; } = string.Empty;
    public string? ClueWord { get; init; }
    public int? ClueNumber { get; init; }
    public int GuessesUsed { get; init; }
}
