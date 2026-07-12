using System.Text.Json;
using System.Text.Json.Serialization;
using Cluely.Domain.Common;
using Cluely.Domain.Room;
using Cluely.Domain.Room.Entities;
using Cluely.Domain.Room.Events;
using Cluely.Domain.Room.ValueObjects;
using Cluely.Infrastructure.Common;
using Cluely.Infrastructure.Persistence.Exceptions;
using Cluely.Infrastructure.Persistence.Models;

namespace Cluely.Infrastructure.Persistence.Mappers;

internal static class RoomMapper
{
    public const int CurrentSnapshotSchemaVersion = 1;

    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = SnapshotSourceGenerationContext.Default,
    };

    public static RoomSnapshot ToSnapshotEntity(this Room room, DateTime? createdAt = null)
    {
        var payload = room.ToPayload();
        return new RoomSnapshot
        {
            RoomId = room.Id.Value,
            RoomCode = room.Code.Value,
            Version = room.Version.Value,
            SnapshotSchemaVersion = CurrentSnapshotSchemaVersion,
            SerializedState = JsonSerializer.Serialize(payload, SnapshotJsonOptions),
            CreatedAt = createdAt ?? DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow,
        };
    }

    public static Room ToDomain(this RoomSnapshot snapshot)
    {
        if (snapshot.SnapshotSchemaVersion != CurrentSnapshotSchemaVersion)
        {
            throw new RoomCustodyException(
                $"Unsupported snapshot schema version {snapshot.SnapshotSchemaVersion} for room {snapshot.RoomId}.");
        }

        RoomSnapshotPayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<RoomSnapshotPayload>(snapshot.SerializedState, SnapshotJsonOptions)
                ?? throw new RoomCustodyException($"Snapshot payload for room {snapshot.RoomId} is empty.");
        }
        catch (JsonException ex)
        {
            throw new RoomCustodyException($"Snapshot payload for room {snapshot.RoomId} is corrupted.", ex);
        }

        return payload.ToDomain(snapshot.RoomId, snapshot.RoomCode, snapshot.Version);
    }

    public static IReadOnlyList<RoomEvent> ToEventEntities(
        this Room room,
        long startingSequence,
        DateTime occurredAt)
    {
        var events = new List<RoomEvent>();
        var sequence = startingSequence;

        foreach (var domainEvent in room.GetPendingEvents())
        {
            sequence++;
            var (eventType, eventData) = RoomEventSerializer.Serialize(domainEvent);
            events.Add(new RoomEvent
            {
                Id = Guid.NewGuid(),
                RoomId = room.Id.Value,
                Sequence = sequence,
                AggregateVersion = room.Version.Value,
                EventType = eventType,
                EventData = eventData,
                OccurredAt = occurredAt,
            });
        }

        return events;
    }

    private static RoomSnapshotPayload ToPayload(this Room room)
    {
        return new RoomSnapshotPayload
        {
            RoomState = (int)room.State,
            StartingTeam = room.StartingTeam.Value,
            WinningTeam = room.WinningTeam?.Value,
            DictionaryRegionCode = room.Dictionary?.Region.Value,
            DictionaryContentVersion = room.Dictionary?.Version.Value,
            Participants = room.Participants.Select(p => new ParticipantPayload
            {
                ParticipantId = p.Id.Value,
                Nickname = p.Nickname,
                Team = p.Team.Value,
                Role = p.Role.Value,
                IsHost = p.IsHost,
            }).ToList(),
            Board = room.Board?.ToPayload(),
            CurrentTurn = room.CurrentTurn?.ToPayload(),
        };
    }

    private static Room ToDomain(this RoomSnapshotPayload payload, Guid roomId, string roomCode, int version)
    {
        var participants = payload.Participants.Select(p => new Participant(
            id: ParticipantId.From(p.ParticipantId),
            nickname: p.Nickname,
            team: TeamParsing.FromStoredValue(p.Team),
            role: Role.From(p.Role),
            isHost: p.IsHost)).ToList();

        DictionaryReference? dictionary = null;
        if (payload.DictionaryRegionCode is not null && payload.DictionaryContentVersion is not null)
        {
            dictionary = DictionaryReference.Create(
                RegionCode.From(payload.DictionaryRegionCode),
                ContentVersion.From(payload.DictionaryContentVersion));
        }

        return new Room(
            id: RoomId.From(roomId),
            code: RoomCode.From(roomCode),
            version: AggregateVersion.From(version),
            state: (RoomState)payload.RoomState,
            participants: participants,
            dictionary: dictionary,
            board: payload.Board?.ToDomain(),
            currentTurn: payload.CurrentTurn?.ToDomain(),
            winningTeam: payload.WinningTeam is not null ? TeamParsing.FromStoredValue(payload.WinningTeam) : null,
            startingTeam: TeamParsing.FromStoredValue(payload.StartingTeam));
    }

    private static Team ToTeam(string value) => TeamParsing.FromStoredValue(value);

    private static BoardPayload ToPayload(this Board board)
    {
        return new BoardPayload
        {
            Cards = board.Cards.Select(c => new CardPayload
            {
                Position = c.Position.Value,
                Word = c.Word,
                Ownership = c.Ownership.Value,
                IsRevealed = c.IsRevealed,
            }).ToList(),
            RedRemaining = board.RedRemaining,
            BlueRemaining = board.BlueRemaining,
        };
    }

    private static Board ToDomain(this BoardPayload payload)
    {
        var cards = payload.Cards.Select(c => new Card(
            position: CardPosition.From(c.Position),
            word: c.Word,
            ownership: ToCardOwnership(c.Ownership),
            isRevealed: c.IsRevealed)).ToList();

        return new Board(cards, payload.RedRemaining, payload.BlueRemaining);
    }

    private static TurnPayload ToPayload(this Turn turn)
    {
        return new TurnPayload
        {
            Number = turn.Number,
            ActiveTeam = turn.ActiveTeam.Value,
            ClueWord = turn.Clue?.Word,
            ClueNumber = turn.Clue?.Number,
            GuessesUsed = turn.GuessesUsed,
        };
    }

    private static Turn ToDomain(this TurnPayload payload)
    {
        Clue? clue = null;
        if (payload.ClueWord is not null && payload.ClueNumber.HasValue)
        {
            clue = Clue.Create(payload.ClueWord, payload.ClueNumber.Value);
        }

        return new Turn(
            number: payload.Number,
            activeTeam: ToTeam(payload.ActiveTeam),
            clue: clue,
            guessesUsed: payload.GuessesUsed);
    }

    private static CardOwnership ToCardOwnership(string value)
    {
        return value switch
        {
            "Red" => CardOwnership.Red,
            "Blue" => CardOwnership.Blue,
            "Neutral" => CardOwnership.Neutral,
            "Assassin" => CardOwnership.Assassin,
            _ => throw new RoomCustodyException($"Unsupported card ownership value '{value}'.")
        };
    }
}

[JsonSerializable(typeof(RoomSnapshotPayload))]
[JsonSerializable(typeof(ParticipantPayload))]
[JsonSerializable(typeof(BoardPayload))]
[JsonSerializable(typeof(CardPayload))]
[JsonSerializable(typeof(TurnPayload))]
internal partial class SnapshotSourceGenerationContext : JsonSerializerContext;

[JsonSerializable(typeof(RoomCreated))]
[JsonSerializable(typeof(PlayerJoined))]
[JsonSerializable(typeof(PlayerLeft))]
[JsonSerializable(typeof(RoomExpired))]
[JsonSerializable(typeof(HostTransferred))]
[JsonSerializable(typeof(PlayerRemovedByHost))]
[JsonSerializable(typeof(RoomClosed))]
[JsonSerializable(typeof(TeamChanged))]
[JsonSerializable(typeof(RoleChanged))]
[JsonSerializable(typeof(DictionarySelected))]
[JsonSerializable(typeof(GameStarted))]
[JsonSerializable(typeof(BoardGenerated))]
[JsonSerializable(typeof(TurnStarted))]
[JsonSerializable(typeof(ClueSubmitted))]
[JsonSerializable(typeof(GuessSubmitted))]
[JsonSerializable(typeof(CardRevealed))]
[JsonSerializable(typeof(TurnEnded))]
[JsonSerializable(typeof(GameFinished))]
internal partial class EventSourceGenerationContext : JsonSerializerContext;
