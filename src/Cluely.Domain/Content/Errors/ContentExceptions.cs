using Cluely.Domain.Common;

namespace Cluely.Domain.Content.Errors;

public sealed class NotOwnerException : DomainException
{
    public NotOwnerException(string message) : base(message)
    {
    }
}

public sealed class VersionImmutableException : DomainException
{
    public VersionImmutableException(string message) : base(message)
    {
    }
}

public sealed class DraftTooSmallException : DomainException
{
    public DraftTooSmallException(string message) : base(message)
    {
    }
}

public sealed class DraftTooLargeException : DomainException
{
    public DraftTooLargeException(string message) : base(message)
    {
    }
}

public sealed class InvalidWordException : DomainException
{
    public InvalidWordException(string message) : base(message)
    {
    }
}

public sealed class DuplicateWordException : DomainException
{
    public DuplicateWordException(string message) : base(message)
    {
    }
}

public sealed class WordNotFoundException : DomainException
{
    public WordNotFoundException(string message) : base(message)
    {
    }
}

public sealed class VisibilityTransitionException : DomainException
{
    public VisibilityTransitionException(string message) : base(message)
    {
    }
}

public sealed class DictionaryLifecycleException : DomainException
{
    public DictionaryLifecycleException(string message) : base(message)
    {
    }
}

public sealed class VersionLifecycleException : DomainException
{
    public VersionLifecycleException(string message) : base(message)
    {
    }
}

public sealed class VersionNotFoundException : DomainException
{
    public VersionNotFoundException(string message) : base(message)
    {
    }
}

public sealed class DraftNotValidatedException : DomainException
{
    public DraftNotValidatedException(string message) : base(message)
    {
    }
}

public sealed class ShareGrantNotFoundException : DomainException
{
    public ShareGrantNotFoundException(string message) : base(message)
    {
    }
}

public sealed class DuplicateShareGrantException : DomainException
{
    public DuplicateShareGrantException(string message) : base(message)
    {
    }
}
