namespace Cluely.Infrastructure.Persistence.Exceptions;

public sealed class RoomCustodyException : Exception
{
    public RoomCustodyException(string message) : base(message)
    {
    }

    public RoomCustodyException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
