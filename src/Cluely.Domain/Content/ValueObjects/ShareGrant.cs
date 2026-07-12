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
        yield return GranteeId;
        yield return GrantedAt;
    }
}
