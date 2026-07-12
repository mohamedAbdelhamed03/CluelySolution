using System.Text.Json;
using Cluely.Domain.Content;
using Cluely.Domain.Common;
using Cluely.Domain.Content.Entities;
using Cluely.Domain.Content.ValueObjects;
using Cluely.Infrastructure.Persistence.Models;
using DictionaryAggregate = Cluely.Domain.Content.Dictionary;

namespace Cluely.Infrastructure.Persistence.Mappers;

internal static class DictionaryMapper
{
    public const int CurrentSnapshotSchemaVersion = 1;

    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static DictionarySnapshot ToSnapshot(DictionaryAggregate dictionary, Guid? idempotencyKey, DateTime createdAt)
    {
        var payload = ToPayload(dictionary);
        var currentLabel = dictionary.CurrentVersionId is null
            ? (int?)null
            : dictionary.Versions.First(version => version.Id == dictionary.CurrentVersionId).Label.Value;

        return new DictionarySnapshot
        {
            DictionaryId = dictionary.Id.Value,
            OwnerId = dictionary.Owner.Value,
            ContentType = dictionary.ContentType.Value,
            Visibility = dictionary.Visibility.Value,
            State = dictionary.State.ToString(),
            Title = dictionary.Metadata.Title.Value,
            Description = dictionary.Metadata.Description.Value,
            Language = dictionary.Metadata.Language.Value,
            Region = dictionary.Metadata.Region?.Value,
            TagsJson = JsonSerializer.Serialize(dictionary.Metadata.Tags.Values, PayloadJsonOptions),
            CurrentVersionId = dictionary.CurrentVersionId?.Value,
            CurrentVersionLabel = currentLabel,
            Version = dictionary.Version.Value,
            IdempotencyKey = idempotencyKey,
            SnapshotSchemaVersion = CurrentSnapshotSchemaVersion,
            SerializedState = JsonSerializer.Serialize(payload, PayloadJsonOptions),
            CreatedAt = createdAt,
            LastModifiedAt = DateTime.UtcNow,
            ShareGrants = dictionary.ShareGrants
                .Select(grant => new DictionaryShareGrantRow
                {
                    DictionaryId = dictionary.Id.Value,
                    GranteeId = grant.GranteeId.Value,
                })
                .ToList(),
        };
    }

    public static DictionaryAggregate ToDomain(DictionarySnapshot snapshot)
    {
        var payload = JsonSerializer.Deserialize<DictionarySnapshotPayload>(snapshot.SerializedState, PayloadJsonOptions)
            ?? throw new InvalidOperationException($"Dictionary {snapshot.DictionaryId} snapshot payload is empty.");

        var metadata = DictionaryMetadata.Create(
            payload.Metadata.Title,
            payload.Metadata.Description,
            payload.Metadata.Tags,
            payload.Metadata.Language,
            payload.Metadata.Region);

        var draft = DictionaryDraft.Rehydrate(
            ToWordSet(payload.Draft.Words),
            Enum.Parse<DraftState>(payload.Draft.State));

        var versions = payload.Versions.Select(version => DictionaryVersion.Rehydrate(
            VersionId.From(version.VersionId),
            DictionaryId.From(snapshot.DictionaryId),
            VersionLabel.From(version.Label),
            ToWordSet(version.Words),
            Enum.Parse<VersionLifecycleState>(version.LifecycleState),
            version.PublishedAt));

        var shareGrants = payload.ShareGrants.Select(grant =>
            ShareGrant.Create(OwnerId.From(grant.GranteeId), grant.GrantedAt));

        var provenance = payload.Provenance is null
            ? null
            : Provenance.ForClone(
                DictionaryId.From(payload.Provenance.SourceDictionaryId),
                VersionId.From(payload.Provenance.SourceVersionId),
                payload.Provenance.ClonedAt);

        return new DictionaryAggregate(
            DictionaryId.From(snapshot.DictionaryId),
            OwnerId.From(snapshot.OwnerId),
            ContentType.From(payload.ContentType),
            Visibility.From(payload.Visibility),
            Enum.Parse<DictionaryState>(payload.State),
            metadata,
            draft,
            provenance,
            payload.CurrentVersionId is null ? null : VersionId.From(payload.CurrentVersionId.Value),
            VersionLabel.From(payload.NextVersionLabel),
            versions,
            shareGrants,
            AggregateVersion.From(snapshot.Version));
    }

    /// <summary>Deserializes the raw snapshot payload for read-model projection (no aggregate build).</summary>
    public static DictionarySnapshotPayload DeserializePayload(string serializedState)
    {
        return JsonSerializer.Deserialize<DictionarySnapshotPayload>(serializedState, PayloadJsonOptions)
            ?? throw new InvalidOperationException("Dictionary snapshot payload is empty.");
    }

    private static WordSet ToWordSet(IEnumerable<string> words)
    {
        return WordSet.FromWords(words.Select(Word.FromRaw));
    }

    private static DictionarySnapshotPayload ToPayload(DictionaryAggregate dictionary)
    {
        return new DictionarySnapshotPayload
        {
            ContentType = dictionary.ContentType.Value,
            Visibility = dictionary.Visibility.Value,
            State = dictionary.State.ToString(),
            Metadata = new DictionaryMetadataPayload
            {
                Title = dictionary.Metadata.Title.Value,
                Description = dictionary.Metadata.Description.Value,
                Tags = dictionary.Metadata.Tags.Values.ToList(),
                Language = dictionary.Metadata.Language.Value,
                Region = dictionary.Metadata.Region?.Value,
            },
            Draft = new DictionaryDraftPayload
            {
                Words = dictionary.Draft.Words.Words.Select(word => word.Value).ToList(),
                State = dictionary.Draft.State.ToString(),
            },
            Provenance = dictionary.Provenance is null
                ? null
                : new DictionaryProvenancePayload
                {
                    SourceDictionaryId = dictionary.Provenance.SourceDictionaryId.Value,
                    SourceVersionId = dictionary.Provenance.SourceVersionId.Value,
                    OriginType = dictionary.Provenance.OriginType.Value,
                    ClonedAt = dictionary.Provenance.ClonedAt,
                },
            CurrentVersionId = dictionary.CurrentVersionId?.Value,
            NextVersionLabel = dictionary.NextVersionLabel.Value,
            Versions = dictionary.Versions.Select(version => new DictionaryVersionPayload
            {
                VersionId = version.Id.Value,
                Label = version.Label.Value,
                Words = version.Words.Words.Select(word => word.Value).ToList(),
                LifecycleState = version.LifecycleState.ToString(),
                PublishedAt = version.PublishedAt,
            }).ToList(),
            ShareGrants = dictionary.ShareGrants.Select(grant => new DictionaryShareGrantPayload
            {
                GranteeId = grant.GranteeId.Value,
                GrantedAt = grant.GrantedAt,
            }).ToList(),
        };
    }
}
