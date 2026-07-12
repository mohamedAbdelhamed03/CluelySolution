using Cluely.Domain.Content;
using Cluely.Domain.Content.ValueObjects;
using Cluely.Infrastructure.Persistence.Exceptions;
using Cluely.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DictionaryAggregate = Cluely.Domain.Content.Dictionary;

namespace Cluely.IntegrationTests.Persistence;

[Collection(nameof(SqlServerTestCollection))]
public sealed class DictionaryPersistenceTests
{
    private readonly SqlServerTestDatabase _database;

    public DictionaryPersistenceTests(SqlServerTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task Migration_CreatesDictionaryTables()
    {
        await using var context = _database.CreateDictionaryContext();

        var count = await context.DbContext.DictionarySnapshots.CountAsync();

        count.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task AddAsync_ThenGetAsync_RecoversAggregate()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = NewDictionary(owner);

        await SaveNewAsync(dictionary);
        var loaded = await LoadAsync(dictionary.Id);

        loaded.Should().NotBeNull();
        loaded!.Owner.Should().Be(owner);
        loaded.Visibility.Should().Be(Visibility.Private);
        loaded.State.Should().Be(DictionaryState.Active);
        loaded.ContentType.Should().Be(ContentType.User);
        loaded.Metadata.Title.Value.Should().Be("Test Dictionary");
        loaded.Draft.Words.Count.Should().Be(0);
        loaded.Provenance.Should().BeNull();
        loaded.Version.Value.Should().Be(dictionary.Version.Value);
    }

    [Fact]
    public async Task AddAsync_RichAggregate_PersistsDraftVersionsAndShares()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var grantee = OwnerId.From(Guid.NewGuid());
        var dictionary = NewDictionary(owner);
        dictionary.AddWords(owner, Words(25));
        var versionId = Publish(dictionary, owner);
        dictionary.AddWords(owner, ["extra"]);
        dictionary.SetVisibility(owner, Visibility.Shared);
        dictionary.ShareWith(owner, grantee, DateTime.UtcNow);

        await SaveNewAsync(dictionary);
        var loaded = await LoadAsync(dictionary.Id);

        loaded!.Versions.Should().ContainSingle();
        loaded.GetVersion(versionId).Words.Count.Should().Be(25);
        loaded.GetVersion(versionId).Label.Value.Should().Be(1);
        loaded.GetVersion(versionId).LifecycleState.Should().Be(VersionLifecycleState.Published);
        loaded.CurrentVersionId.Should().Be(versionId);
        loaded.Draft.Words.Count.Should().Be(26);
        loaded.Visibility.Should().Be(Visibility.Shared);
        loaded.ShareGrants.Should().ContainSingle(grant => grant.GranteeId == grantee);
        loaded.Version.Value.Should().Be(dictionary.Version.Value);
    }

    [Fact]
    public async Task UpdateAsync_ConcurrentUpdate_ThrowsConcurrencyException()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = NewDictionary(owner);
        await SaveNewAsync(dictionary);

        await using var contextA = _database.CreateDictionaryContext();
        await using var contextB = _database.CreateDictionaryContext();
        var copyA = await contextA.Repository.GetAsync(dictionary.Id);
        var copyB = await contextB.Repository.GetAsync(dictionary.Id);

        copyA!.Archive(owner);
        await contextA.Repository.UpdateAsync(copyA);

        copyB!.Archive(owner);
        Func<Task> conflicting = () => contextB.Repository.UpdateAsync(copyB);

        await conflicting.Should().ThrowAsync<DictionaryConcurrencyException>();
    }

    [Fact]
    public async Task UpdateAsync_SequentialUpdates_Persist()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = NewDictionary(owner);
        await SaveNewAsync(dictionary);

        await using (var context = _database.CreateDictionaryContext())
        {
            var loaded = await context.Repository.GetAsync(dictionary.Id);
            loaded!.AddWords(owner, Words(25));
            await context.Repository.UpdateAsync(loaded);
        }

        var reloaded = await LoadAsync(dictionary.Id);
        reloaded!.Draft.Words.Count.Should().Be(25);
    }

    [Fact]
    public async Task AddAsync_SameIdempotencyKey_IsDeterministicAndPreventsDuplicates()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var dictionary = NewDictionary(owner);
        var key = Guid.NewGuid();

        await using (var context = _database.CreateDictionaryContext())
        {
            await context.Repository.AddAsync(dictionary, key);
            var byKey = await context.Repository.GetByIdempotencyKeyAsync(key);
            byKey.Should().NotBeNull();
            byKey!.Id.Should().Be(dictionary.Id);
        }

        await using var duplicateContext = _database.CreateDictionaryContext();
        Func<Task> duplicate = () => duplicateContext.Repository.AddAsync(NewDictionary(owner), key);

        await duplicate.Should().ThrowAsync<DictionaryPersistenceException>();
    }

    [Fact]
    public async Task CloneAsync_PersistsProvenance_AndIsIdempotentByKey()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var cloner = OwnerId.From(Guid.NewGuid());
        var source = NewDictionary(owner);
        source.AddWords(owner, Words(25));
        var versionId = Publish(source, owner);
        await SaveNewAsync(source);

        var clonedAt = new DateTime(2026, 07, 12, 09, 15, 00, DateTimeKind.Utc);
        var clone = DictionaryAggregate.CloneFrom(DictionaryId.New(), cloner, source, versionId, source.Metadata, clonedAt);
        var key = Guid.NewGuid();

        await using (var context = _database.CreateDictionaryContext())
        {
            await context.Repository.AddAsync(clone, key);
        }

        var loaded = await LoadAsync(clone.Id);
        loaded!.Owner.Should().Be(cloner);
        loaded.Draft.Words.Count.Should().Be(25);
        loaded.Provenance.Should().NotBeNull();
        loaded.Provenance!.SourceDictionaryId.Should().Be(source.Id);
        loaded.Provenance.SourceVersionId.Should().Be(versionId);
        loaded.Provenance.OriginType.Should().Be(OriginType.Clone);
        loaded.Provenance.ClonedAt.Should().Be(clonedAt);

        await using var keyContext = _database.CreateDictionaryContext();
        var byKey = await keyContext.Repository.GetByIdempotencyKeyAsync(key);
        byKey!.Id.Should().Be(clone.Id);
    }

    [Fact]
    public async Task ReadModel_FiltersByVisibility()
    {
        var owner = OwnerId.From(Guid.NewGuid());
        var requester = OwnerId.From(Guid.NewGuid());

        var mine = PublishedDictionary(owner, Visibility.Private, out var mineVersion);
        var publicOther = PublishedDictionary(OwnerId.From(Guid.NewGuid()), Visibility.Public, out _);
        var privateOther = PublishedDictionary(OwnerId.From(Guid.NewGuid()), Visibility.Private, out _);
        var sharedOther = PublishedDictionary(OwnerId.From(Guid.NewGuid()), Visibility.Shared, out _, requester);

        await SaveNewAsync(mine);
        await SaveNewAsync(publicOther);
        await SaveNewAsync(privateOther);
        await SaveNewAsync(sharedOther);

        await using var context = _database.CreateDictionaryContext();

        var owned = await context.ReadModel.GetOwnedAsync(owner);
        owned.Select(d => d.DictionaryId).Should().Contain(mine.Id.Value);

        var discoverable = await context.ReadModel.GetDiscoverableAsync(requester);
        var discoverableIds = discoverable.Select(d => d.DictionaryId).ToList();
        discoverableIds.Should().Contain(publicOther.Id.Value);
        discoverableIds.Should().Contain(sharedOther.Id.Value);
        discoverableIds.Should().NotContain(privateOther.Id.Value);
        discoverableIds.Should().NotContain(mine.Id.Value);

        var publicDetails = await context.ReadModel.GetDetailsAsync(publicOther.Id, requester);
        publicDetails.Should().NotBeNull();
        publicDetails!.Versions.Should().ContainSingle();

        var hiddenDetails = await context.ReadModel.GetDetailsAsync(privateOther.Id, requester);
        hiddenDetails.Should().BeNull();

        var ownerVersions = await context.ReadModel.GetVersionsAsync(mine.Id, owner);
        ownerVersions.Should().NotBeNull();
        ownerVersions!.Select(v => v.VersionId).Should().Contain(mineVersion.Value);
    }

    private async Task SaveNewAsync(DictionaryAggregate dictionary)
    {
        await using var context = _database.CreateDictionaryContext();
        await context.Repository.AddAsync(dictionary, Guid.NewGuid());
    }

    private async Task<DictionaryAggregate?> LoadAsync(DictionaryId id)
    {
        await using var context = _database.CreateDictionaryContext();
        return await context.Repository.GetAsync(id);
    }

    private static DictionaryAggregate NewDictionary(OwnerId owner) =>
        DictionaryAggregate.Create(DictionaryId.New(), owner, ContentType.User, Metadata());

    private static DictionaryMetadata Metadata() =>
        DictionaryMetadata.Create("Test Dictionary", "description", ["party", "family"], "en", "US");

    private static IEnumerable<string> Words(int count) =>
        Enumerable.Range(1, count).Select(index => $"word{index}");

    private static VersionId Publish(DictionaryAggregate dictionary, OwnerId owner)
    {
        var versionId = VersionId.New();
        dictionary.Publish(owner, versionId, DateTime.UtcNow);
        return versionId;
    }

    private static DictionaryAggregate PublishedDictionary(
        OwnerId owner,
        Visibility visibility,
        out VersionId versionId,
        OwnerId? grantee = null)
    {
        var dictionary = NewDictionary(owner);
        dictionary.AddWords(owner, Words(25));
        versionId = Publish(dictionary, owner);
        if (visibility != Visibility.Private)
        {
            dictionary.SetVisibility(owner, visibility);
        }

        if (grantee is not null)
        {
            dictionary.ShareWith(owner, grantee, DateTime.UtcNow);
        }

        return dictionary;
    }
}
