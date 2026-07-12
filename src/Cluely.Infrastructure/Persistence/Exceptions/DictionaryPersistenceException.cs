namespace Cluely.Infrastructure.Persistence.Exceptions;

public class DictionaryPersistenceException : Exception
{
    public DictionaryPersistenceException(string message) : base(message)
    {
    }

    public DictionaryPersistenceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public sealed class DictionaryConcurrencyException : DictionaryPersistenceException
{
    public DictionaryConcurrencyException(string message) : base(message)
    {
    }

    public DictionaryConcurrencyException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
