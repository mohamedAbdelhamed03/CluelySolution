using System.Text.Json;
using Cluely.Application.Common.Ports.Content;
using Cluely.Application.Common.ReadModels;
using Cluely.Domain.Content.ValueObjects;
using Cluely.Infrastructure.Persistence;
using Cluely.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Cluely.Infrastructure.ReadModels;

/// <summary>
/// Persistence-backed discovery read model. Summary lists are projected directly from queryable columns
/// (no aggregate rehydration, <c>AsNoTracking</c>); single-dictionary detail/version views deserialize
/// the one snapshot payload. Visibility is filtered server-side. Returns DTOs only.
/// </summary>
public sealed class DictionaryReadModelProvider : IDictionaryReadModelProvider
{
    private readonly CluelyDbContext _dbContext;

    public DictionaryReadModelProvider(CluelyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<IReadOnlyList<DictionarySummaryReadModel>> GetOwnedAsync(
        OwnerId owner,
        CancellationToken cancellationToken = default)
    {
        return QuerySummariesAsync(
            _dbContext.DictionarySnapshots.AsNoTracking().Where(d => d.OwnerId == owner.Value),
            cancellationToken);
    }

    public Task<IReadOnlyList<DictionarySummaryReadModel>> GetDiscoverableAsync(
        OwnerId requester,
        CancellationToken cancellationToken = default)
    {
        return QuerySummariesAsync(
            _dbContext.DictionarySnapshots.AsNoTracking().Where(d => d.OwnerId != requester.Value
                && (d.Visibility == "Public"
                    || (d.Visibility == "Shared" && d.ShareGrants.Any(g => g.GranteeId == requester.Value)))),
            cancellationToken);
    }

    public async Task<DictionaryDetailsReadModel?> GetDetailsAsync(
        DictionaryId dictionaryId,
        OwnerId requester,
        CancellationToken cancellationToken = default)
    {
        var row = await LoadViewable(dictionaryId, requester, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var payload = DictionaryMapper.DeserializePayload(row.SerializedState);
        var versions = VisibleVersions(payload, row.OwnerId, row.CurrentVersionId, requester.Value);

        return new DictionaryDetailsReadModel(
            row.DictionaryId,
            row.OwnerId,
            payload.Metadata.Title,
            payload.Metadata.Description,
            payload.Metadata.Tags,
            payload.Metadata.Language,
            payload.Metadata.Region,
            row.Visibility,
            row.ContentType,
            row.CurrentVersionId,
            row.CurrentVersionLabel,
            versions);
    }

    public async Task<IReadOnlyList<DictionaryVersionReadModel>?> GetVersionsAsync(
        DictionaryId dictionaryId,
        OwnerId requester,
        CancellationToken cancellationToken = default)
    {
        var row = await LoadViewable(dictionaryId, requester, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var payload = DictionaryMapper.DeserializePayload(row.SerializedState);
        return VisibleVersions(payload, row.OwnerId, row.CurrentVersionId, requester.Value);
    }

    private static async Task<IReadOnlyList<DictionarySummaryReadModel>> QuerySummariesAsync(
        IQueryable<Persistence.Models.DictionarySnapshot> query,
        CancellationToken cancellationToken)
    {
        var rows = await query
            .Select(d => new SummaryRow
            {
                DictionaryId = d.DictionaryId,
                OwnerId = d.OwnerId,
                Title = d.Title,
                Description = d.Description,
                TagsJson = d.TagsJson,
                Language = d.Language,
                Region = d.Region,
                Visibility = d.Visibility,
                ContentType = d.ContentType,
                CurrentVersionId = d.CurrentVersionId,
                CurrentVersionLabel = d.CurrentVersionLabel,
            })
            .ToListAsync(cancellationToken);

        return rows.Select(ToSummary).ToList();
    }

    private async Task<DetailRow?> LoadViewable(
        DictionaryId dictionaryId,
        OwnerId requester,
        CancellationToken cancellationToken)
    {
        return await _dbContext.DictionarySnapshots
            .AsNoTracking()
            .Where(d => d.DictionaryId == dictionaryId.Value
                && (d.OwnerId == requester.Value
                    || d.Visibility == "Public"
                    || (d.Visibility == "Shared" && d.ShareGrants.Any(g => g.GranteeId == requester.Value))))
            .Select(d => new DetailRow
            {
                DictionaryId = d.DictionaryId,
                OwnerId = d.OwnerId,
                Visibility = d.Visibility,
                ContentType = d.ContentType,
                CurrentVersionId = d.CurrentVersionId,
                CurrentVersionLabel = d.CurrentVersionLabel,
                SerializedState = d.SerializedState,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static IReadOnlyList<DictionaryVersionReadModel> VisibleVersions(
        Persistence.Models.DictionarySnapshotPayload payload,
        Guid ownerId,
        Guid? currentVersionId,
        Guid requesterId)
    {
        var versions = payload.Versions.AsEnumerable();

        // The owner sees the full history; other authorized viewers see the current published version only.
        if (requesterId != ownerId)
        {
            versions = versions.Where(v => currentVersionId.HasValue && v.VersionId == currentVersionId.Value);
        }

        return versions
            .Select(v => new DictionaryVersionReadModel(v.VersionId, v.Label, v.PublishedAt, v.Words.Count, v.LifecycleState))
            .ToList();
    }

    private static DictionarySummaryReadModel ToSummary(SummaryRow row)
    {
        var tags = JsonSerializer.Deserialize<List<string>>(row.TagsJson) ?? [];
        return new DictionarySummaryReadModel(
            row.DictionaryId,
            row.OwnerId,
            row.Title,
            row.Description,
            tags,
            row.Language,
            row.Region,
            row.Visibility,
            row.ContentType,
            row.CurrentVersionId,
            row.CurrentVersionLabel);
    }

    private sealed class SummaryRow
    {
        public Guid DictionaryId { get; init; }
        public Guid OwnerId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string TagsJson { get; init; } = "[]";
        public string Language { get; init; } = string.Empty;
        public string? Region { get; init; }
        public string Visibility { get; init; } = string.Empty;
        public string ContentType { get; init; } = string.Empty;
        public Guid? CurrentVersionId { get; init; }
        public int? CurrentVersionLabel { get; init; }
    }

    private sealed class DetailRow
    {
        public Guid DictionaryId { get; init; }
        public Guid OwnerId { get; init; }
        public string Visibility { get; init; } = string.Empty;
        public string ContentType { get; init; } = string.Empty;
        public Guid? CurrentVersionId { get; init; }
        public int? CurrentVersionLabel { get; init; }
        public string SerializedState { get; init; } = string.Empty;
    }
}
