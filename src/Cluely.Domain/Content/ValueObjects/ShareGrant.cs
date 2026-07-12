using Cluely.Domain.Common;
using Cluely.Domain.Content.ValueObjects;

namespace Cluely.Domain.Content.ValueObjects;

public sealed class ShareGrant : ValueObject
{
    public OwnerId GranteeId { get; }
    public DateTime GrantedAt { get; }

    private ShareGrant(OwnerId granteeId, DateTime grantedAt)
    {
        GranteeId = granteeId;
        GrantedAt = grantedAt;
    }

    public static ShareGrant Create(OwnerId granteeId, DateTime grantedAt)
    {
        return new ShareGrant(granteeId, grantedAt);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        // Identity is the grantee: a dictionary has at most one grant per account (TD-001). The grant
        // timestamp is informational and must not participate in equality, otherwise repeated shares of
        // the same grantee would create duplicate grants and make revocation non-deterministic.
        yield return GranteeId;
    }
}
