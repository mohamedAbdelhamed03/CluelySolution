using Cluely.Application.Common.Ports.Content;
using Cluely.Application.Common.ReadModels;
using Cluely.Application.Content.Discovery;
using Cluely.Domain.Content.ValueObjects;

namespace Cluely.UnitTests.Content.Discovery;

/// <summary>
/// In-memory reference implementation of <see cref="IDictionaryReadModelProvider"/> for handler tests.
/// It applies the production <see cref="DictionaryVisibilityPolicy"/>, so the visibility contract it
/// enforces is exactly the one the persistence-backed provider must satisfy.
/// </summary>
internal sealed class FakeDictionaryReadModelProvider : IDictionaryReadModelProvider
{
    private sealed record Entry(
        Guid DictionaryId,
        Guid OwnerId,
        string Visibility,
        IReadOnlyList<Guid> Grantees,
        DictionarySummaryReadModel Summary,
        IReadOnlyList<DictionaryVersionReadModel> AllVersions,
        Guid? CurrentVersionId);

    private readonly List<Entry> _entries = [];

    public FakeDictionaryReadModelProvider Seed(
        Guid dictionaryId,
        Guid ownerId,
        string visibility,
        IReadOnlyList<Guid>? grantees = null,
        IReadOnlyList<DictionaryVersionReadModel>? versions = null,
        Guid? currentVersionId = null,
        string title = "Dictionary")
    {
        grantees ??= [];
        versions ??= [];
        var currentLabel = currentVersionId is null
            ? null
            : versions.FirstOrDefault(v => v.VersionId == currentVersionId)?.Label;
        var summary = new DictionarySummaryReadModel(
            dictionaryId, ownerId, title, "desc", ["tag"], "en", "US",
            visibility, "User", currentVersionId, currentLabel);
        _entries.Add(new Entry(dictionaryId, ownerId, visibility, grantees, summary, versions, currentVersionId));
        return this;
    }

    public Task<IReadOnlyList<DictionarySummaryReadModel>> GetOwnedAsync(
        OwnerId owner,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<DictionarySummaryReadModel>>(
            _entries.Where(entry => entry.OwnerId == owner.Value).Select(entry => entry.Summary).ToList());
    }

    public Task<IReadOnlyList<DictionarySummaryReadModel>> GetDiscoverableAsync(
        OwnerId requester,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<DictionarySummaryReadModel>>(
            _entries
                .Where(entry => DictionaryVisibilityPolicy.IsDiscoverableBy(
                    entry.OwnerId, entry.Visibility, entry.Grantees, requester.Value))
                .Select(entry => entry.Summary)
                .ToList());
    }

    public Task<DictionaryDetailsReadModel?> GetDetailsAsync(
        DictionaryId dictionaryId,
        OwnerId requester,
        CancellationToken cancellationToken = default)
    {
        var entry = Find(dictionaryId, requester);
        if (entry is null)
        {
            return Task.FromResult<DictionaryDetailsReadModel?>(null);
        }

        var summary = entry.Summary;
        var details = new DictionaryDetailsReadModel(
            summary.DictionaryId, summary.OwnerId, summary.Title, summary.Description, summary.Tags,
            summary.Language, summary.Region, summary.Visibility, summary.ContentType,
            summary.CurrentVersionId, summary.CurrentVersionLabel, VisibleVersions(entry, requester.Value));
        return Task.FromResult<DictionaryDetailsReadModel?>(details);
    }

    public Task<IReadOnlyList<DictionaryVersionReadModel>?> GetVersionsAsync(
        DictionaryId dictionaryId,
        OwnerId requester,
        CancellationToken cancellationToken = default)
    {
        var entry = Find(dictionaryId, requester);
        return Task.FromResult<IReadOnlyList<DictionaryVersionReadModel>?>(
            entry is null ? null : VisibleVersions(entry, requester.Value));
    }

    private Entry? Find(DictionaryId dictionaryId, OwnerId requester)
    {
        var entry = _entries.FirstOrDefault(e => e.DictionaryId == dictionaryId.Value);
        if (entry is null
            || !DictionaryVisibilityPolicy.CanView(entry.OwnerId, entry.Visibility, entry.Grantees, requester.Value))
        {
            return null;
        }

        return entry;
    }

    private static IReadOnlyList<DictionaryVersionReadModel> VisibleVersions(Entry entry, Guid requesterId)
    {
        // The owner sees the full history; other authorized viewers see the current published version only.
        return requesterId == entry.OwnerId
            ? entry.AllVersions
            : entry.AllVersions.Where(version => version.VersionId == entry.CurrentVersionId).ToList();
    }
}
