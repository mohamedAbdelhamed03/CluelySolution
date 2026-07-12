using Cluely.Domain.Common;
using Cluely.Domain.Content.ValueObjects;

namespace Cluely.Domain.Content.Events;

public sealed record DictionaryCreated(
    DictionaryId DictionaryId,
    OwnerId OwnerId,
    ContentType ContentType,
    DictionaryMetadata Metadata) : IContentDomainEvent;

public sealed record DictionaryRenamed(
    DictionaryId DictionaryId,
    Title Title) : IContentDomainEvent;

public sealed record WordsChanged(
    DictionaryId DictionaryId,
    int WordCount) : IContentDomainEvent;

public sealed record DraftDiscarded(
    DictionaryId DictionaryId,
    int WordCount) : IContentDomainEvent;

public sealed record VersionPublished(
    DictionaryId DictionaryId,
    VersionId VersionId,
    VersionLabel Label,
    int WordCount) : IContentDomainEvent;

public sealed record VisibilityChanged(
    DictionaryId DictionaryId,
    Visibility Visibility) : IContentDomainEvent;

public sealed record DictionaryShared(
    DictionaryId DictionaryId,
    OwnerId GranteeId) : IContentDomainEvent;

public sealed record ShareRevoked(
    DictionaryId DictionaryId,
    OwnerId GranteeId) : IContentDomainEvent;

public sealed record DictionaryCloned(
    DictionaryId DictionaryId,
    DictionaryId SourceDictionaryId,
    VersionId SourceVersionId) : IContentDomainEvent;

public sealed record DictionaryArchived(DictionaryId DictionaryId) : IContentDomainEvent;

public sealed record DictionaryRestored(DictionaryId DictionaryId) : IContentDomainEvent;

public sealed record DictionaryDeletionRequested(DictionaryId DictionaryId) : IContentDomainEvent;

public sealed record DictionaryDeletionCancelled(DictionaryId DictionaryId) : IContentDomainEvent;

public sealed record DictionaryDeleted(DictionaryId DictionaryId) : IContentDomainEvent;

public sealed record VersionSubmittedForReview(
    DictionaryId DictionaryId,
    VersionId VersionId) : IContentDomainEvent;

public sealed record ReviewApproved(
    DictionaryId DictionaryId,
    VersionId VersionId) : IContentDomainEvent;

public sealed record ReviewRejected(
    DictionaryId DictionaryId,
    VersionId VersionId) : IContentDomainEvent;

public sealed record VersionBlocked(
    DictionaryId DictionaryId,
    VersionId VersionId) : IContentDomainEvent;

public sealed record VersionUnblocked(
    DictionaryId DictionaryId,
    VersionId VersionId) : IContentDomainEvent;

public sealed record VersionRetired(
    DictionaryId DictionaryId,
    VersionId VersionId) : IContentDomainEvent;
