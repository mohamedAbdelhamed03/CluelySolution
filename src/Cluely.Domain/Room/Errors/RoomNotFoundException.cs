using Cluely.Domain.Common;

namespace Cluely.Domain.Room.Errors;

public class RoomNotFoundException : DomainException
{
    public RoomNotFoundException(string message) : base(message)
    {
    }
}

public class InvalidRoomCodeException : DomainException
{
    public InvalidRoomCodeException(string message) : base(message)
    {
    }
}

public class RoomExpiredException : DomainException
{
    public RoomExpiredException(string message) : base(message)
    {
    }
}

public class RoomFullException : DomainException
{
    public RoomFullException(string message) : base(message)
    {
    }
}

public class GameInProgressCannotJoinException : DomainException
{
    public GameInProgressCannotJoinException(string message) : base(message)
    {
    }
}

public class RoomClosedException : DomainException
{
    public RoomClosedException(string message) : base(message)
    {
    }
}

public class NicknameRequiredException : DomainException
{
    public NicknameRequiredException(string message) : base(message)
    {
    }
}

public class NicknameInvalidException : DomainException
{
    public NicknameInvalidException(string message) : base(message)
    {
    }
}

public class DuplicateNicknameException : DomainException
{
    public DuplicateNicknameException(string message) : base(message)
    {
    }
}

public class NotAMemberException : DomainException
{
    public NotAMemberException(string message) : base(message)
    {
    }
}

public class TeamInvalidException : DomainException
{
    public TeamInvalidException(string message) : base(message)
    {
    }
}

public class TeamChangeNotAllowedException : DomainException
{
    public TeamChangeNotAllowedException(string message) : base(message)
    {
    }
}

public class TeamNotSelectedException : DomainException
{
    public TeamNotSelectedException(string message) : base(message)
    {
    }
}

public class RoleAlreadyTakenException : DomainException
{
    public RoleAlreadyTakenException(string message) : base(message)
    {
    }
}

public class RoleChangeNotAllowedException : DomainException
{
    public RoleChangeNotAllowedException(string message) : base(message)
    {
    }
}

public class DictionaryNotFoundException : DomainException
{
    public DictionaryNotFoundException(string message) : base(message)
    {
    }
}

public class NotRoomHostException : DomainException
{
    public NotRoomHostException(string message) : base(message)
    {
    }
}

public class GameAlreadyStartedException : DomainException
{
    public GameAlreadyStartedException(string message) : base(message)
    {
    }
}

public class MatchConfigurationInvalidException : DomainException
{
    public MatchConfigurationInvalidException(string message) : base(message)
    {
    }
}

public class DictionaryTooSmallException : DomainException
{
    public DictionaryTooSmallException(string message) : base(message)
    {
    }
}

public class NotSpymasterException : DomainException
{
    public NotSpymasterException(string message) : base(message)
    {
    }
}

public class NotYourTurnException : DomainException
{
    public NotYourTurnException(string message) : base(message)
    {
    }
}

public class InvalidClueException : DomainException
{
    public InvalidClueException(string message) : base(message)
    {
    }
}

public class ClueAlreadyGivenException : DomainException
{
    public ClueAlreadyGivenException(string message) : base(message)
    {
    }
}

public class NotOperativeException : DomainException
{
    public NotOperativeException(string message) : base(message)
    {
    }
}

public class NoActiveClueException : DomainException
{
    public NoActiveClueException(string message) : base(message)
    {
    }
}

public class CardAlreadyRevealedException : DomainException
{
    public CardAlreadyRevealedException(string message) : base(message)
    {
    }
}

public class GuessLimitReachedException : DomainException
{
    public GuessLimitReachedException(string message) : base(message)
    {
    }
}

public class InvalidGuessException : DomainException
{
    public InvalidGuessException(string message) : base(message)
    {
    }
}

public class EndTurnBeforeGuessException : DomainException
{
    public EndTurnBeforeGuessException(string message) : base(message)
    {
    }
}

public class NotActiveTeamException : DomainException
{
    public NotActiveTeamException(string message) : base(message)
    {
    }
}

public class GameAlreadyFinishedException : DomainException
{
    public GameAlreadyFinishedException(string message) : base(message)
    {
    }
}

public class GameNotStartedException : DomainException
{
    public GameNotStartedException(string message) : base(message)
    {
    }
}

public class ActionOutOfPhaseException : DomainException
{
    public ActionOutOfPhaseException(string message) : base(message)
    {
    }
}
