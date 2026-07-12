namespace Cluely.Domain.Content;

public enum DictionaryState
{
    Active,
    Archived,
    PendingDeletion,
    Deleted
}

public enum DraftState
{
    Draft,
    Validated
}

public enum VersionLifecycleState
{
    Published,
    PendingReview,
    Discoverable,
    Deprecated,
    Archived,
    Retired,
    Blocked
}
