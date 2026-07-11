using Cluely.Domain.Common;
using Cluely.Domain.Room.Entities;
using Cluely.Domain.Room.Errors;
using Cluely.Domain.Room.Events;
using Cluely.Domain.Room.ValueObjects;

namespace Cluely.Domain.Room;

public enum RoomState
{
    Lobby,
    InProgress,
    Finished,
    Closed
}

public enum GamePhase
{
    AwaitingClue,
    AwaitingGuess
}

public sealed class Room
{
    private readonly List<IDomainEvent> _pendingEvents = [];
    private readonly List<Participant> _participants = [];

    public RoomId Id { get; }
    public RoomCode Code { get; }
    public RoomState State { get; private set; }
    public AggregateVersion Version { get; private set; }
    public IReadOnlyList<Participant> Participants => _participants.AsReadOnly();
    public DictionaryReference? Dictionary { get; private set; }
    public Board? Board { get; private set; }
    public Turn? CurrentTurn { get; private set; }
    public Team? WinningTeam { get; private set; }
    public Team StartingTeam { get; private set; } = Team.Red;
    public ParticipantId HostId => _participants.Single(p => p.IsHost).Id;

    private Room(RoomId id, RoomCode code, Participant host)
    {
        Id = id;
        Code = code;
        State = RoomState.Lobby;
        Version = AggregateVersion.Initial();
        _participants.Add(host);
        _pendingEvents.Add(new RoomCreated(id, code, host.Id, host.Nickname));
    }

    public static Room Create(RoomId id, RoomCode code, string hostNickname)
    {
        var hostId = ParticipantId.New();
        var host = Participant.Create(hostId, hostNickname, isHost: true);
        return new Room(id, code, host);
    }

    public IReadOnlyList<IDomainEvent> GetPendingEvents() => _pendingEvents.AsReadOnly();
    public void ClearPendingEvents() => _pendingEvents.Clear();

    public void Join(ParticipantId participantId, string nickname)
    {
        if (State != RoomState.Lobby && State != RoomState.Finished)
        {
            throw new GameInProgressCannotJoinException("Cannot join during an active match");
        }

        if (_participants.Any(p => p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DuplicateNicknameException("Nickname is already in use");
        }

        if (_participants.Count >= 10) // Assuming ROOM_MAX_PLAYERS is 10 per docs
        {
            throw new RoomFullException("Room is full");
        }

        var participant = Participant.Create(participantId, nickname, isHost: false);
        _participants.Add(participant);
        _pendingEvents.Add(new PlayerJoined(Id, participantId, nickname));
        IncrementVersion();
    }

    public void Leave(ParticipantId participantId)
    {
        var participant = _participants.SingleOrDefault(p => p.Id == participantId);
        if (participant == null)
        {
            throw new NotAMemberException("Participant is not a member of this room");
        }

        _participants.Remove(participant);
        _pendingEvents.Add(new PlayerLeft(Id, participantId, "Left voluntarily"));

        if (participant.IsHost && _participants.Any())
        {
            var newHost = _participants.First();
            newHost.SetHost(true);
            _pendingEvents.Add(new HostTransferred(Id, participantId, newHost.Id));
        }

        if (!_participants.Any())
        {
            State = RoomState.Closed;
            _pendingEvents.Add(new RoomClosed(Id, "Room is empty"));
        }

        IncrementVersion();
    }

    public void AssignTeam(ParticipantId participantId, Team team)
    {
        if (State == RoomState.InProgress)
        {
            throw new TeamChangeNotAllowedException("Cannot change team during an active match");
        }

        var participant = _participants.SingleOrDefault(p => p.Id == participantId);
        if (participant == null)
        {
            throw new NotAMemberException("Participant is not a member of this room");
        }

        var previousTeam = participant.Team;
        participant.SetTeam(team);
        _pendingEvents.Add(new TeamChanged(Id, participantId, previousTeam, team));
        IncrementVersion();
    }

    public void AssignRole(ParticipantId participantId, Role role)
    {
        if (State == RoomState.InProgress)
        {
            throw new RoleChangeNotAllowedException("Cannot change role during an active match");
        }

        var participant = _participants.SingleOrDefault(p => p.Id == participantId);
        if (participant == null)
        {
            throw new NotAMemberException("Participant is not a member of this room");
        }

        if (role == Role.Spymaster)
        {
            var existingSpymaster = _participants.SingleOrDefault(p => p.Team == participant.Team && p.Role == Role.Spymaster && p.Id != participantId);
            if (existingSpymaster != null)
            {
                throw new RoleAlreadyTakenException("Team already has a Spymaster");
            }
        }

        var previousRole = participant.Role;
        participant.SetRole(role);
        _pendingEvents.Add(new RoleChanged(Id, participantId, previousRole, role));
        IncrementVersion();
    }

    public void SelectDictionary(DictionaryReference dictionary)
    {
        Dictionary = dictionary;
        _pendingEvents.Add(new DictionarySelected(Id, dictionary));
        IncrementVersion();
    }

    public void StartMatch(ParticipantId requestorId)
    {
        if (HostId != requestorId)
        {
            throw new NotRoomHostException("Only the host can start a match");
        }

        if (State == RoomState.InProgress)
        {
            throw new GameAlreadyStartedException("Match is already in progress");
        }

        if (State == RoomState.Closed)
        {
            throw new RoomClosedException("Room is closed");
        }

        ValidateMatchConfiguration();

        State = RoomState.InProgress;
        _pendingEvents.Add(new GameStarted(Id));

        GenerateBoard();
        StartTurn();

        IncrementVersion();
    }

    private void ValidateMatchConfiguration()
    {
        var redTeam = _participants.Where(p => p.Team == Team.Red).ToList();
        var blueTeam = _participants.Where(p => p.Team == Team.Blue).ToList();

        if (redTeam.Count < 2 || blueTeam.Count < 2 || _participants.Count < 4)
        {
            throw new MatchConfigurationInvalidException("Need at least 4 players with 2 per team");
        }

        if (!redTeam.Any(p => p.Role == Role.Spymaster) || !blueTeam.Any(p => p.Role == Role.Spymaster))
        {
            throw new MatchConfigurationInvalidException("Each team must have one Spymaster");
        }

        if (Dictionary == null)
        {
            throw new MatchConfigurationInvalidException("Dictionary not selected");
        }
    }

    private void GenerateBoard()
    {
        // Note: In a real implementation, this would use a domain service to get words from the dictionary
        // For now, we'll use a placeholder to simulate board generation
        var words = Enumerable.Range(0, 25).Select(i => $"Word{i}").ToList();
        var ownerships = Enumerable.Repeat(CardOwnership.Red, 9)
            .Concat(Enumerable.Repeat(CardOwnership.Blue, 8))
            .Concat(Enumerable.Repeat(CardOwnership.Neutral, 7))
            .Concat(new[] { CardOwnership.Assassin })
            .OrderBy(_ => Guid.NewGuid())
            .ToList();

        var cardData = words.Select((word, i) => (Position: CardPosition.From(i), Word: word, Ownership: ownerships[i])).ToList();

        Board = Board.Create(cardData);
        StartingTeam = ownerships[0] == CardOwnership.Red ? Team.Red : Team.Blue;
        _pendingEvents.Add(new BoardGenerated(Id, cardData, StartingTeam));
    }

    public void SubmitClue(ParticipantId requestorId, Clue clue)
    {
        var requestor = _participants.SingleOrDefault(p => p.Id == requestorId);
        if (requestor == null)
        {
            throw new NotAMemberException("Participant is not a member of this room");
        }

        if (State != RoomState.InProgress)
        {
            if (State == RoomState.Finished) throw new GameAlreadyFinishedException("Game already finished");
            throw new GameNotStartedException("Game not started yet");
        }

        if (CurrentTurn == null) throw new InvalidOperationException("No active turn");

        if (!CurrentTurn.IsAwaitingClue)
        {
            throw new ClueAlreadyGivenException("Clue already given this turn");
        }

        if (requestor.Role != Role.Spymaster)
        {
            throw new NotSpymasterException("Only Spymaster can submit clues");
        }

        if (requestor.Team != CurrentTurn.ActiveTeam)
        {
            throw new NotYourTurnException("Not your team's turn");
        }

        CurrentTurn.SubmitClue(clue);
        _pendingEvents.Add(new ClueSubmitted(Id, CurrentTurn.ActiveTeam, clue));
        IncrementVersion();
    }

    public void SubmitGuess(ParticipantId requestorId, CardPosition position)
    {
        var requestor = _participants.SingleOrDefault(p => p.Id == requestorId);
        if (requestor == null)
        {
            throw new NotAMemberException("Participant is not a member of this room");
        }

        if (State != RoomState.InProgress)
        {
            if (State == RoomState.Finished) throw new GameAlreadyFinishedException("Game already finished");
            throw new GameNotStartedException("Game not started yet");
        }

        if (CurrentTurn == null) throw new InvalidOperationException("No active turn");
        if (Board == null) throw new InvalidOperationException("No active board");

        if (!CurrentTurn.IsAwaitingGuess)
        {
            throw new NoActiveClueException("No active clue to guess on");
        }

        if (requestor.Role != Role.Operative)
        {
            throw new NotOperativeException("Only Operatives can guess");
        }

        if (requestor.Team != CurrentTurn.ActiveTeam)
        {
            throw new NotYourTurnException("Not your team's turn");
        }

        if (Board.Cards.Single(c => c.Position == position).IsRevealed)
        {
            throw new CardAlreadyRevealedException("Card already revealed");
        }

        if (CurrentTurn.GuessesUsed >= CurrentTurn.GuessAllowance)
        {
            throw new GuessLimitReachedException("Guess limit reached");
        }

        _pendingEvents.Add(new GuessSubmitted(Id, requestorId, position));
        var revealedCard = Board.RevealCard(position);
        _pendingEvents.Add(new CardRevealed(Id, position, revealedCard.Word, revealedCard.Ownership, Board.RedRemaining, Board.BlueRemaining));
        CurrentTurn.IncrementGuessCount();

        // Check for game end
        if (CheckGameEnd(revealedCard.Ownership))
        {
            return;
        }

        // Check if turn should end
        if (ShouldTurnEnd(revealedCard.Ownership))
        {
            EndTurn(CurrentTurn.ActiveTeam, GetTurnEndReason(revealedCard.Ownership));
        }
        else
        {
            IncrementVersion();
        }
    }

    private bool CheckGameEnd(CardOwnership revealedOwnership)
    {
        if (Board == null) return false;

        if (revealedOwnership == CardOwnership.Assassin)
        {
            var winningTeam = CurrentTurn?.ActiveTeam == Team.Red ? Team.Blue : Team.Red;
            FinishGame(winningTeam, "Assassin revealed");
            return true;
        }

        if (Board.RedRemaining == 0)
        {
            FinishGame(Team.Red, "All red agents revealed");
            return true;
        }

        if (Board.BlueRemaining == 0)
        {
            FinishGame(Team.Blue, "All blue agents revealed");
            return true;
        }

        return false;
    }

    private bool ShouldTurnEnd(CardOwnership revealedOwnership)
    {
        if (CurrentTurn == null || Board == null) return true;

        if (revealedOwnership != CurrentTurn.ActiveTeam && revealedOwnership != CardOwnership.Neutral)
            return true;

        if (CurrentTurn.GuessesUsed >= CurrentTurn.GuessAllowance)
            return true;

        return false;
    }

    private string GetTurnEndReason(CardOwnership revealedOwnership)
    {
        if (revealedOwnership == CardOwnership.Assassin) return "Assassin revealed";
        if (revealedOwnership == CardOwnership.Neutral) return "Neutral card revealed";
        if (revealedOwnership != CurrentTurn?.ActiveTeam) return "Opponent's card revealed";
        return "Guess limit reached";
    }

    public void EndTurn(ParticipantId requestorId)
    {
        var requestor = _participants.SingleOrDefault(p => p.Id == requestorId);
        if (requestor == null)
        {
            throw new NotAMemberException("Participant is not a member of this room");
        }

        if (State != RoomState.InProgress)
        {
            if (State == RoomState.Finished) throw new GameAlreadyFinishedException("Game already finished");
            throw new GameNotStartedException("Game not started yet");
        }

        if (CurrentTurn == null) throw new InvalidOperationException("No active turn");

        if (requestor.Team != CurrentTurn.ActiveTeam)
        {
            throw new NotYourTurnException("Not your team's turn");
        }

        if (CurrentTurn.GuessesUsed < 1)
        {
            throw new EndTurnBeforeGuessException("Must make at least one guess before ending turn");
        }

        EndTurn(CurrentTurn.ActiveTeam, "Turn ended voluntarily");
    }

    private void EndTurn(Team team, string reason)
    {
        if (CurrentTurn == null) return;
        _pendingEvents.Add(new TurnEnded(Id, team, reason));
        StartTurn();
        IncrementVersion();
    }

    private void StartTurn()
    {
        var nextTeam = CurrentTurn == null
            ? StartingTeam
            : (CurrentTurn.ActiveTeam == Team.Red ? Team.Blue : Team.Red);
        var turnNumber = CurrentTurn == null ? 1 : CurrentTurn.Number + 1;
        CurrentTurn = Turn.Start(turnNumber, nextTeam);
        _pendingEvents.Add(new TurnStarted(Id, nextTeam, turnNumber));
    }

    private void FinishGame(Team? winningTeam, string reason)
    {
        State = RoomState.Finished;
        WinningTeam = winningTeam;
        _pendingEvents.Add(new GameFinished(Id, winningTeam, reason));
        IncrementVersion();
    }

    private void IncrementVersion()
    {
        Version = Version.Next();
    }
}
