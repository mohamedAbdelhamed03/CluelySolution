namespace Cluely.Application.Content.Discovery;

/// <summary>
/// Server-side visibility rule for dictionary discovery (Feature Specification §7). This is the single
/// authoritative decision for "may this requester see this dictionary"; the read-model provider applies
/// it before returning any result. Pure and side-effect free.
/// </summary>
public static class DictionaryVisibilityPolicy
{
    /// <summary>
    /// Whether <paramref name="requesterId"/> may view a dictionary. The owner may always view their
    /// own content; public content is viewable by any requester; shared content is viewable by its
    /// grantees; private content is viewable only by the owner.
    /// </summary>
    public static bool CanView(
        Guid ownerId,
        string visibility,
        IReadOnlyCollection<Guid> shareGrantees,
        Guid requesterId)
    {
        if (requesterId == ownerId)
        {
            return true;
        }

        return visibility switch
        {
            "Public" => true,
            "Shared" => shareGrantees.Contains(requesterId),
            _ => false
        };
    }

    /// <summary>
    /// Whether a dictionary appears in <paramref name="requesterId"/>'s discovery catalog. Excludes the
    /// requester's own dictionaries (those belong to the "mine" catalog) and returns only content the
    /// requester is otherwise authorized to view.
    /// </summary>
    public static bool IsDiscoverableBy(
        Guid ownerId,
        string visibility,
        IReadOnlyCollection<Guid> shareGrantees,
        Guid requesterId)
    {
        if (requesterId == ownerId)
        {
            return false;
        }

        return CanView(ownerId, visibility, shareGrantees, requesterId);
    }
}
