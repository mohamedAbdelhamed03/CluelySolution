using Cluely.Domain.Common;

namespace Cluely.Domain.Content.ValueObjects;

/// <summary>
/// Identifies the single account that owns a Content Platform Dictionary (ADR-011 §6.3, AI-CP-1).
/// It wraps the durable account identity supplied by the authentication seam
/// (<c>ICurrentUserAccessor.UserId</c>, ADR-009); the Content domain never mints owners of its own.
/// </summary>
public sealed class OwnerId : ValueObject
{
    /// <summary>The underlying durable account identity.</summary>
    public Guid Value { get; }

    private OwnerId(Guid value)
    {
        Value = value;
    }

    /// <summary>Creates an <see cref="OwnerId"/> from an existing account identity.</summary>
    /// <param name="value">A non-empty account identity.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty.</exception>
    public static OwnerId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Owner id cannot be empty.", nameof(value));
        }

        return new OwnerId(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
