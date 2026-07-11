namespace Cluely.Infrastructure.Delivery.Contracts;

public sealed record RoomProjectionDto(
    string RoomCode,
    string State,
    int AggregateVersion,
    IReadOnlyList<ParticipantProjectionDto> Participants,
    DictionaryProjectionDto? Dictionary,
    BoardProjectionDto? Board,
    TurnProjectionDto? CurrentTurn,
    string? WinningTeam,
    string StartingTeam);

public sealed record ParticipantProjectionDto(
    Guid ParticipantId,
    string Nickname,
    string Team,
    string Role,
    bool IsHost);

public sealed record DictionaryProjectionDto(
    string RegionCode,
    string ContentVersion);

public sealed record BoardProjectionDto(
    IReadOnlyList<CardProjectionDto> Cards,
    int RedRemaining,
    int BlueRemaining);

public sealed record CardProjectionDto(
    int Position,
    string Word,
    bool IsRevealed,
    string? Ownership);

public sealed record TurnProjectionDto(
    int Number,
    string ActiveTeam,
    string? ClueWord,
    int? ClueNumber,
    int GuessesUsed);
