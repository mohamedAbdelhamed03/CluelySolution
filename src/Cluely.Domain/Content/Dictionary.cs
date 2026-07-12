using Cluely.Domain.Common;
using Cluely.Domain.Content.Entities;
using Cluely.Domain.Content.Errors;
using Cluely.Domain.Content.Events;
using Cluely.Domain.Content.ValueObjects;

namespace Cluely.Domain.Content;

public sealed class Dictionary : AggregateRoot<DictionaryId>
{
    private readonly List<DictionaryVersion> _versions = [];
    private readonly HashSet<ShareGrant> _shareGrants = [];
    private DictionaryState? _stateBeforePendingDeletion;

    public OwnerId Owner { get; }
    public ContentType ContentType { get; }
    public Visibility Visibility { get; private set; }
    public DictionaryState State { get; private set; }
    public DictionaryMetadata Metadata { get; private set; }
    public DictionaryDraft Draft { get; }

    /// <summary>
    /// Exposes the Draft size without leaking Draft internals into Application handlers.
    /// </summary>
    public int DraftWordCount => Draft.Words.Count;
    public Provenance? Provenance { get; }
    public VersionId? CurrentVersionId { get; private set; }
    public VersionLabel NextVersionLabel { get; private set; }

    public IReadOnlyList<DictionaryVersion> Versions => _versions.AsReadOnly();
    public IReadOnlyCollection<ShareGrant> ShareGrants => _shareGrants;

    private Dictionary(
        DictionaryId id,
        OwnerId owner,
        ContentType contentType,
        DictionaryMetadata metadata,
        Provenance? provenance)
        : base(id)
    {
        Owner = owner;
        ContentType = contentType;
        Metadata = metadata;
        Provenance = provenance;
        Visibility = Visibility.Private;
        State = DictionaryState.Active;
        Draft = DictionaryDraft.Empty();
        NextVersionLabel = VersionLabel.Initial();
        AddDomainEvent(new DictionaryCreated(id, owner, contentType, metadata));
    }

    internal Dictionary(
        DictionaryId id,
        OwnerId owner,
        ContentType contentType,
        Visibility visibility,
        DictionaryState state,
        DictionaryMetadata metadata,
        DictionaryDraft draft,
        Provenance? provenance,
        VersionId? currentVersionId,
        VersionLabel nextVersionLabel,
        IEnumerable<DictionaryVersion> versions,
        IEnumerable<ShareGrant> shareGrants,
        AggregateVersion version)
        : base(id, version)
    {
        Owner = owner;
        ContentType = contentType;
        Visibility = visibility;
        State = state;
        Metadata = metadata;
        Draft = draft;
        Provenance = provenance;
        CurrentVersionId = currentVersionId;
        NextVersionLabel = nextVersionLabel;
        _versions.AddRange(versions);
        foreach (var grant in shareGrants)
        {
            _shareGrants.Add(grant);
        }
    }

    public static Dictionary Create(
        DictionaryId id,
        OwnerId owner,
        ContentType contentType,
        DictionaryMetadata metadata)
    {
        return new Dictionary(id, owner, contentType, metadata, provenance: null);
    }

    public static Dictionary CloneFrom(
        DictionaryId newId,
        OwnerId newOwner,
        Dictionary source,
        DictionaryVersion sourceVersion,
        DictionaryMetadata metadata)
    {
        if (sourceVersion.DictionaryId != source.Id)
        {
            throw new VersionNotFoundException("Source version does not belong to the source dictionary.");
        }

        var provenance = Provenance.From(sourceVersion.Id);
        var clone = new Dictionary(newId, newOwner, ContentType.User, metadata, provenance);
        clone.Draft.SetWords(sourceVersion.Words.Copy());
        clone.AddDomainEvent(new DictionaryCloned(
            newId,
            source.Id,
            sourceVersion.Id));
        return clone;
    }

    public void UpdateMetadata(OwnerId actor, DictionaryMetadata metadata)
    {
        EnsureOwner(actor);
        EnsureAuthoringAllowed();

        var previousTitle = Metadata.Title;
        Metadata = metadata;

        if (previousTitle != metadata.Title)
        {
            AddDomainEvent(new DictionaryRenamed(Id, metadata.Title));
        }

        IncrementVersion();
    }

    public void AddWords(OwnerId actor, IEnumerable<string> words)
    {
        EnsureOwner(actor);
        EnsureAuthoringAllowed();

        try
        {
            Draft.SetWords(Draft.Words.AddWords(words));
        }
        catch (ArgumentException ex)
        {
            throw new InvalidWordException(ex.Message);
        }
        catch (DuplicateWordException)
        {
            throw;
        }

        AddDomainEvent(new WordsChanged(Id, Draft.Words.Count));
        IncrementVersion();
    }

    public void RemoveWord(OwnerId actor, Word word)
    {
        EnsureOwner(actor);
        EnsureAuthoringAllowed();

        Draft.SetWords(Draft.Words.RemoveWord(word));
        AddDomainEvent(new WordsChanged(Id, Draft.Words.Count));
        IncrementVersion();
    }

    public void UpdateWord(OwnerId actor, Word existingWord, string newRaw)
    {
        EnsureOwner(actor);
        EnsureAuthoringAllowed();

        try
        {
            Draft.SetWords(Draft.Words.ReplaceWord(existingWord, newRaw));
        }
        catch (ArgumentException ex)
        {
            throw new InvalidWordException(ex.Message);
        }
        catch (DuplicateWordException)
        {
            throw;
        }

        AddDomainEvent(new WordsChanged(Id, Draft.Words.Count));
        IncrementVersion();
    }

    public void DiscardDraft(OwnerId actor)
    {
        EnsureOwner(actor);
        EnsureAuthoringAllowed();

        var baseline = GetLastPublishedWords() ?? WordSet.Empty();
        Draft.SetWords(baseline.Copy());
        AddDomainEvent(new DraftDiscarded(Id, Draft.Words.Count));
        IncrementVersion();
    }

    public DraftValidationReport ValidateDraft(OwnerId actor)
    {
        EnsureOwner(actor);
        EnsureAuthoringAllowed();

        var report = DraftValidationReport.FromWordSet(Draft.Words);
        if (report.IsValid)
        {
            Draft.MarkValidated();
        }
        else
        {
            Draft.MarkDraft();
        }

        return report;
    }

    public VersionId Publish(OwnerId actor, VersionId versionId, DateTime publishedAt)
    {
        EnsureOwner(actor);
        EnsureAuthoringAllowed();

        var report = DraftValidationReport.FromWordSet(Draft.Words);
        if (!report.IsValid)
        {
            Draft.MarkDraft();
            throw new DraftTooSmallException(string.Join(' ', report.Errors));
        }

        Draft.MarkValidated();

        DeprecateCurrentVersion();

        var initialState = Visibility == Visibility.Public
            ? VersionLifecycleState.PendingReview
            : VersionLifecycleState.Published;

        var publishedVersion = DictionaryVersion.Publish(
            versionId,
            Id,
            NextVersionLabel,
            Draft.Words,
            initialState,
            publishedAt);

        _versions.Add(publishedVersion);
        NextVersionLabel = NextVersionLabel.Next();
        Draft.MarkDraft();
        RecomputeCurrentVersion();
        AddDomainEvent(new VersionPublished(
            Id,
            versionId,
            publishedVersion.Label,
            publishedVersion.Words.Count));
        IncrementVersion();
        return versionId;
    }

    public void SetVisibility(OwnerId actor, Visibility visibility)
    {
        EnsureOwner(actor);
        EnsureAuthoringAllowed();

        if (visibility == Visibility.Public && !_versions.Any())
        {
            throw new VisibilityTransitionException(
                "Public visibility requires at least one published version.");
        }

        Visibility = visibility;
        AddDomainEvent(new VisibilityChanged(Id, visibility));
        IncrementVersion();
    }

    public void ShareWith(OwnerId actor, OwnerId grantee, DateTime grantedAt)
    {
        EnsureOwner(actor);
        EnsureAuthoringAllowed();

        if (Visibility != Visibility.Shared && Visibility != Visibility.Public)
        {
            throw new VisibilityTransitionException(
                "Sharing is only available when visibility is Shared or Public.");
        }

        if (grantee == Owner)
        {
            throw new DuplicateShareGrantException("Owner already has access.");
        }

        var grant = ShareGrant.Create(grantee, grantedAt);
        if (!_shareGrants.Add(grant))
        {
            throw new DuplicateShareGrantException("Dictionary is already shared with this account.");
        }

        AddDomainEvent(new DictionaryShared(Id, grantee));
        IncrementVersion();
    }

    public void RevokeShare(OwnerId actor, OwnerId grantee)
    {
        EnsureOwner(actor);
        EnsureAuthoringAllowed();

        var grant = _shareGrants.SingleOrDefault(g => g.GranteeId == grantee);
        if (grant is null)
        {
            throw new ShareGrantNotFoundException("Share grant was not found.");
        }

        _shareGrants.Remove(grant);
        AddDomainEvent(new ShareRevoked(Id, grantee));
        IncrementVersion();
    }

    public void Archive(OwnerId actor)
    {
        EnsureOwner(actor);

        if (State != DictionaryState.Active)
        {
            throw new DictionaryLifecycleException("Only active dictionaries can be archived.");
        }

        State = DictionaryState.Archived;
        AddDomainEvent(new DictionaryArchived(Id));
        IncrementVersion();
    }

    public void Restore(OwnerId actor)
    {
        EnsureOwner(actor);

        if (State != DictionaryState.Archived)
        {
            throw new DictionaryLifecycleException("Only archived dictionaries can be restored.");
        }

        State = DictionaryState.Active;
        AddDomainEvent(new DictionaryRestored(Id));
        IncrementVersion();
    }

    public void RequestDeletion(OwnerId actor)
    {
        EnsureOwner(actor);

        if (State is DictionaryState.PendingDeletion or DictionaryState.Deleted)
        {
            throw new DictionaryLifecycleException("Deletion has already been requested or completed.");
        }

        _stateBeforePendingDeletion = State;
        State = DictionaryState.PendingDeletion;
        AddDomainEvent(new DictionaryDeletionRequested(Id));
        IncrementVersion();
    }

    public void CancelDeletion(OwnerId actor)
    {
        EnsureOwner(actor);

        if (State != DictionaryState.PendingDeletion)
        {
            throw new DictionaryLifecycleException("No pending deletion to cancel.");
        }

        State = _stateBeforePendingDeletion ?? DictionaryState.Active;
        _stateBeforePendingDeletion = null;
        AddDomainEvent(new DictionaryDeletionCancelled(Id));
        IncrementVersion();
    }

    public void Report(OwnerId reporter)
    {
        if (Visibility == Visibility.Private)
        {
            throw new VisibilityTransitionException(
                "Only shared or public dictionaries can be reported.");
        }

        AddDomainEvent(new DictionaryReported(Id, reporter));
        IncrementVersion();
    }

    public void CompleteDeletion(OwnerId actor)
    {
        EnsureOwner(actor);

        if (State != DictionaryState.PendingDeletion)
        {
            throw new DictionaryLifecycleException("Deletion can only complete from pending deletion.");
        }

        State = DictionaryState.Deleted;
        AddDomainEvent(new DictionaryDeleted(Id));
        IncrementVersion();
    }

    public void SubmitVersionForReview(OwnerId actor, VersionId versionId)
    {
        EnsureOwner(actor);
        EnsureAuthoringAllowed();

        if (Visibility != Visibility.Public)
        {
            throw new VisibilityTransitionException("Review applies only to public dictionaries.");
        }

        var version = GetRequiredVersion(versionId);
        if (version.LifecycleState != VersionLifecycleState.Published)
        {
            throw new VersionLifecycleException("Only published versions can be submitted for review.");
        }

        version.TransitionTo(VersionLifecycleState.PendingReview);
        RecomputeCurrentVersion();
        AddDomainEvent(new VersionSubmittedForReview(Id, versionId));
        IncrementVersion();
    }

    public void ApproveReview(ModeratorId moderator, VersionId versionId)
    {
        ArgumentNullException.ThrowIfNull(moderator);

        var version = GetRequiredVersion(versionId);
        if (version.LifecycleState != VersionLifecycleState.PendingReview)
        {
            throw new VersionLifecycleException("Version is not pending review.");
        }

        version.TransitionTo(VersionLifecycleState.Discoverable);
        RecomputeCurrentVersion();
        AddDomainEvent(new ReviewApproved(Id, versionId));
        IncrementVersion();
    }

    public void RejectReview(ModeratorId moderator, VersionId versionId)
    {
        ArgumentNullException.ThrowIfNull(moderator);

        var version = GetRequiredVersion(versionId);
        if (version.LifecycleState != VersionLifecycleState.PendingReview)
        {
            throw new VersionLifecycleException("Version is not pending review.");
        }

        version.TransitionTo(VersionLifecycleState.Published);
        RecomputeCurrentVersion();
        AddDomainEvent(new ReviewRejected(Id, versionId));
        IncrementVersion();
    }

    public void BlockVersion(ModeratorId moderator, VersionId versionId)
    {
        ArgumentNullException.ThrowIfNull(moderator);

        var version = GetRequiredVersion(versionId);
        if (version.LifecycleState is VersionLifecycleState.Retired or VersionLifecycleState.Deprecated)
        {
            throw new VersionLifecycleException("Version cannot be blocked from its current state.");
        }

        version.TransitionTo(VersionLifecycleState.Blocked);
        RecomputeCurrentVersion();
        AddDomainEvent(new VersionBlocked(Id, versionId));
        IncrementVersion();
    }

    public void UnblockVersion(ModeratorId moderator, VersionId versionId)
    {
        ArgumentNullException.ThrowIfNull(moderator);

        var version = GetRequiredVersion(versionId);
        if (version.LifecycleState != VersionLifecycleState.Blocked)
        {
            throw new VersionLifecycleException("Version is not blocked.");
        }

        version.TransitionTo(VersionLifecycleState.PendingReview);
        RecomputeCurrentVersion();
        AddDomainEvent(new VersionUnblocked(Id, versionId));
        IncrementVersion();
    }

    public void RetireVersion(ModeratorId moderator, VersionId versionId)
    {
        ArgumentNullException.ThrowIfNull(moderator);

        var version = GetRequiredVersion(versionId);
        if (version.LifecycleState == VersionLifecycleState.Retired)
        {
            throw new VersionLifecycleException("Version is already retired.");
        }

        version.TransitionTo(VersionLifecycleState.Retired);
        RecomputeCurrentVersion();
        AddDomainEvent(new VersionRetired(Id, versionId));
        IncrementVersion();
    }

    public DictionaryVersion GetVersion(VersionId versionId)
    {
        return GetRequiredVersion(versionId);
    }

    private void DeprecateCurrentVersion()
    {
        if (CurrentVersionId is null)
        {
            return;
        }

        var current = GetRequiredVersion(CurrentVersionId);
        if (current.LifecycleState is VersionLifecycleState.Published or VersionLifecycleState.Discoverable)
        {
            current.TransitionTo(VersionLifecycleState.Deprecated);
        }
    }

    private void RecomputeCurrentVersion()
    {
        CurrentVersionId = _versions
            .Where(version => version.CanServeAsCurrent())
            .OrderByDescending(version => version.Label.Value)
            .Select(version => version.Id)
            .FirstOrDefault();
    }

    private WordSet? GetLastPublishedWords()
    {
        var latest = _versions
            .OrderByDescending(version => version.Label.Value)
            .FirstOrDefault();

        return latest?.Words.Copy();
    }

    private DictionaryVersion GetRequiredVersion(VersionId versionId)
    {
        var version = _versions.SingleOrDefault(v => v.Id == versionId);
        if (version is null)
        {
            throw new VersionNotFoundException("Version was not found.");
        }

        return version;
    }

    private void EnsureOwner(OwnerId actor)
    {
        if (actor != Owner)
        {
            throw new NotOwnerException("Only the dictionary owner may perform this action.");
        }
    }

    private void EnsureAuthoringAllowed()
    {
        if (State is DictionaryState.Archived or DictionaryState.PendingDeletion or DictionaryState.Deleted)
        {
            throw new DictionaryLifecycleException("Dictionary is not available for authoring.");
        }
    }
}
