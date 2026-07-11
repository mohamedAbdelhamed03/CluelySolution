using Cluely.Domain.Common;
using Cluely.Domain.Room.Entities;
using Cluely.Domain.Room.ValueObjects;

namespace Cluely.Domain.Room.Events;

public sealed record RoomCreated(RoomId RoomId, RoomCode RoomCode, ParticipantId HostId, string HostNickname) : IDomainEvent;

public sealed record PlayerJoined(RoomId RoomId, ParticipantId ParticipantId, string Nickname) : IDomainEvent;

public sealed record PlayerLeft(RoomId RoomId, ParticipantId ParticipantId, string Reason) : IDomainEvent;

public sealed record RoomExpired(RoomId RoomId, string Reason) : IDomainEvent;

public sealed record HostTransferred(RoomId RoomId, ParticipantId PreviousHostId, ParticipantId NewHostId) : IDomainEvent;

public sealed record PlayerRemovedByHost(RoomId RoomId, ParticipantId RemovedParticipantId) : IDomainEvent;

public sealed record RoomClosed(RoomId RoomId, string Reason) : IDomainEvent;

public sealed record TeamChanged(RoomId RoomId, ParticipantId ParticipantId, Team PreviousTeam, Team NewTeam) : IDomainEvent;

public sealed record RoleChanged(RoomId RoomId, ParticipantId ParticipantId, Role PreviousRole, Role NewRole) : IDomainEvent;

public sealed record DictionarySelected(RoomId RoomId, DictionaryReference Dictionary) : IDomainEvent;

public sealed record GameStarted(RoomId RoomId) : IDomainEvent;

public sealed record BoardGenerated(RoomId RoomId, IReadOnlyList<(CardPosition Position, string Word, CardOwnership Ownership)> Cards, Team StartingTeam) : IDomainEvent;

public sealed record TurnStarted(RoomId RoomId, Team ActiveTeam, int TurnNumber) : IDomainEvent;

public sealed record ClueSubmitted(RoomId RoomId, Team ActiveTeam, Clue Clue) : IDomainEvent;

public sealed record GuessSubmitted(RoomId RoomId, ParticipantId ParticipantId, CardPosition Position) : IDomainEvent;

public sealed record CardRevealed(RoomId RoomId, CardPosition Position, string Word, CardOwnership Ownership, int RedRemaining, int BlueRemaining) : IDomainEvent;

public sealed record TurnEnded(RoomId RoomId, Team EndedTeam, string Reason) : IDomainEvent;

public sealed record GameFinished(RoomId RoomId, Team? WinningTeam, string Reason) : IDomainEvent;
