namespace Cluely.Application.Common.ReadModels;

public sealed record RoomSummaryReadModel(
    Guid RoomId,
    string RoomCode,
    string State,
    int AggregateVersion);

public sealed record RoomProjectionReadModel(
    Guid RoomId,
    string RoomCode,
    string State,
    int AggregateVersion,
    IReadOnlyList<ParticipantReadModel> Participants,
    DictionaryReadModel? Dictionary,
    BoardReadModel? Board,
    TurnReadModel? CurrentTurn,
    string? WinningTeam,
    string StartingTeam);

public sealed record ParticipantReadModel(
    Guid ParticipantId,
    string Nickname,
    string Team,
    string Role,
    bool IsHost);

public sealed record DictionaryReadModel(string RegionCode, string ContentVersion);

public sealed record BoardReadModel(
    IReadOnlyList<CardReadModel> Cards,
    int RedRemaining,
    int BlueRemaining);

public sealed record CardReadModel(
    int Position,
    string Word,
    bool IsRevealed,
    string? Ownership);

public sealed record TurnReadModel(
    int Number,
    string ActiveTeam,
    string? ClueWord,
    int? ClueNumber,
    int GuessesUsed);
