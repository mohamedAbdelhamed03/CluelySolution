namespace Cluely.Api.Contracts.Responses;

/// <summary>
/// Response returned when a room is created.
/// </summary>
public sealed record CreateRoomResponse(Guid RoomId, string RoomCode, Guid HostParticipantId);

/// <summary>
/// Response returned when a player joins a room.
/// </summary>
public sealed record JoinRoomResponse(Guid RoomId, Guid ParticipantId);

/// <summary>
/// Summary information about a room.
/// </summary>
public sealed record RoomSummaryResponse(
    Guid RoomId,
    string RoomCode,
    string State,
    int AggregateVersion);

/// <summary>
/// Role-filtered room projection for read-only queries.
/// </summary>
public sealed record RoomProjectionResponse(
    Guid RoomId,
    string RoomCode,
    string State,
    int AggregateVersion,
    IReadOnlyList<ParticipantResponse> Participants,
    DictionaryResponse? Dictionary,
    BoardResponse? Board,
    TurnResponse? CurrentTurn,
    string? WinningTeam,
    string StartingTeam);

public sealed record ParticipantResponse(
    Guid ParticipantId,
    string Nickname,
    string Team,
    string Role,
    bool IsHost);

public sealed record DictionaryResponse(string RegionCode, string ContentVersion);

public sealed record BoardResponse(
    IReadOnlyList<CardResponse> Cards,
    int RedRemaining,
    int BlueRemaining);

public sealed record CardResponse(
    int Position,
    string Word,
    bool IsRevealed,
    string? Ownership);

public sealed record TurnResponse(
    int Number,
    string ActiveTeam,
    string? ClueWord,
    int? ClueNumber,
    int GuessesUsed);

public sealed record ParticipantsResponse(IReadOnlyList<ParticipantResponse> Participants);

public sealed record HealthResponse(string Status, DateTime UtcTimestamp);

public sealed record RegisterUserResponse(Guid UserId, string Email);

public sealed record LoginUserResponse(
    Guid UserId,
    string Email,
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);

public sealed record RefreshTokenResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);

public sealed record CurrentUserResponse(Guid UserId, string Email, string AccountStatus);
