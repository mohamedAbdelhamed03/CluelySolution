using Cluely.Domain.Common;
using Cluely.Domain.Room.Entities;
using Cluely.Domain.Room.ValueObjects;

namespace Cluely.Domain.Room.Events;

public sealed record RoomCreated(RoomId RoomId, RoomCode RoomCode, ParticipantId HostId, string HostNickname) : IRoomDomainEvent;

public sealed record PlayerJoined(RoomId RoomId, ParticipantId ParticipantId, string Nickname) : IRoomDomainEvent;

public sealed record PlayerLeft(RoomId RoomId, ParticipantId ParticipantId, string Reason) : IRoomDomainEvent;

public sealed record RoomExpired(RoomId RoomId, string Reason) : IRoomDomainEvent;

public sealed record HostTransferred(RoomId RoomId, ParticipantId PreviousHostId, ParticipantId NewHostId) : IRoomDomainEvent;

public sealed record PlayerRemovedByHost(RoomId RoomId, ParticipantId RemovedParticipantId) : IRoomDomainEvent;

public sealed record RoomClosed(RoomId RoomId, string Reason) : IRoomDomainEvent;

public sealed record TeamChanged(RoomId RoomId, ParticipantId ParticipantId, Team PreviousTeam, Team NewTeam) : IRoomDomainEvent;

public sealed record RoleChanged(RoomId RoomId, ParticipantId ParticipantId, Role PreviousRole, Role NewRole) : IRoomDomainEvent;

public sealed record DictionarySelected(RoomId RoomId, DictionaryReference Dictionary) : IRoomDomainEvent;

public sealed record GameStarted(RoomId RoomId) : IRoomDomainEvent;

public sealed record BoardGenerated(RoomId RoomId, IReadOnlyList<(CardPosition Position, string Word, CardOwnership Ownership)> Cards, Team StartingTeam) : IRoomDomainEvent;

public sealed record TurnStarted(RoomId RoomId, Team ActiveTeam, int TurnNumber) : IRoomDomainEvent;

public sealed record ClueSubmitted(RoomId RoomId, Team ActiveTeam, Clue Clue) : IRoomDomainEvent;

public sealed record GuessSubmitted(RoomId RoomId, ParticipantId ParticipantId, CardPosition Position) : IRoomDomainEvent;

public sealed record CardRevealed(RoomId RoomId, CardPosition Position, string Word, CardOwnership Ownership, int RedRemaining, int BlueRemaining) : IRoomDomainEvent;

public sealed record TurnEnded(RoomId RoomId, Team EndedTeam, string Reason) : IRoomDomainEvent;

public sealed record GameFinished(RoomId RoomId, Team? WinningTeam, string Reason) : IRoomDomainEvent;
