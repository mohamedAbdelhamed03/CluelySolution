using Cluely.Domain.Common;

namespace Cluely.Domain.Content.ValueObjects;

/// <summary>
/// Identifies a platform-trusted moderator principal for restricted lifecycle-only commands (REC-1).
/// Authorization is enforced at the application boundary; the domain accepts this principal type
/// only for moderation operations that must not be owner-scoped.
/// </summary>
public sealed class ModeratorId : ValueObject
{
    public Guid Value { get; }

    private ModeratorId(Guid value)
    {
        Value = value;
    }

    public static ModeratorId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Moderator id cannot be empty.", nameof(value));
        }

        return new ModeratorId(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
