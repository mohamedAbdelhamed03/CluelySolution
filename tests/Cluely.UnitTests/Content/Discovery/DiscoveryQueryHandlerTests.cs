using Cluely.Application.Common.ReadModels;
using Cluely.Application.Content.Discovery.GetDictionaryDetails;
using Cluely.Application.Content.Discovery.GetDictionaryVersions;
using Cluely.Application.Content.Discovery.GetDiscoverableDictionaries;
using Cluely.Application.Content.Discovery.GetMyDictionaries;
using Cluely.UnitTests.Content.Handlers;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content.Discovery;

public sealed class DiscoveryQueryHandlerTests
{
    private readonly FakeDictionaryReadModelProvider _provider = new();
    private readonly FakeCurrentUserAccessor _currentUser = new();
    private readonly Guid _userId = Guid.NewGuid();

    public DiscoveryQueryHandlerTests()
    {
        _currentUser.UserId = _userId;
    }

    // --- GetMyDictionaries ---

    [Fact]
    public async Task GetMyDictionaries_ShouldReturnOnlyOwnedDictionaries()
    {
        _provider
            .Seed(Guid.NewGuid(), _userId, "Private", title: "Mine A")
            .Seed(Guid.NewGuid(), _userId, "Public", title: "Mine B")
            .Seed(Guid.NewGuid(), Guid.NewGuid(), "Public", title: "Someone else");

        var handler = new GetMyDictionariesHandler(_provider, _currentUser, new GetMyDictionariesQueryValidator());
        var result = await handler.HandleAsync(new GetMyDictionariesQuery(Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Dictionaries.Select(d => d.Title).Should().BeEquivalentTo(["Mine A", "Mine B"]);
    }

    [Fact]
    public async Task GetMyDictionaries_WhenUnauthenticated_ShouldFail()
    {
        _currentUser.UserId = null;

        var handler = new GetMyDictionariesHandler(_provider, _currentUser, new GetMyDictionariesQueryValidator());
        var result = await handler.HandleAsync(new GetMyDictionariesQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Unauthorized");
    }

    // --- GetDiscoverableDictionaries (visibility filtering) ---

    [Fact]
    public async Task GetDiscoverable_ShouldReturnPublicAndSharedButNotPrivateOrOwn()
    {
        var stranger = Guid.NewGuid();
        _provider
            .Seed(Guid.NewGuid(), _userId, "Public", title: "My own public")                 // excluded: mine
            .Seed(Guid.NewGuid(), stranger, "Public", title: "Stranger public")               // included
            .Seed(Guid.NewGuid(), stranger, "Private", title: "Stranger private")             // excluded
            .Seed(Guid.NewGuid(), stranger, "Shared", grantees: [_userId], title: "Shared to me")   // included
            .Seed(Guid.NewGuid(), stranger, "Shared", grantees: [Guid.NewGuid()], title: "Shared to other"); // excluded

        var handler = new GetDiscoverableDictionariesHandler(_provider, _currentUser, new GetDiscoverableDictionariesQueryValidator());
        var result = await handler.HandleAsync(new GetDiscoverableDictionariesQuery(Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Dictionaries.Select(d => d.Title).Should().BeEquivalentTo(["Stranger public", "Shared to me"]);
    }

    [Fact]
    public async Task GetDiscoverable_WhenUnauthenticated_ShouldFail()
    {
        _currentUser.UserId = null;

        var handler = new GetDiscoverableDictionariesHandler(_provider, _currentUser, new GetDiscoverableDictionariesQueryValidator());
        var result = await handler.HandleAsync(new GetDiscoverableDictionariesQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Unauthorized");
    }

    // --- GetDictionaryDetails ---

    [Fact]
    public async Task GetDetails_AsOwner_ShouldReturnMappedSummaryAndFullHistory()
    {
        var dictionaryId = Guid.NewGuid();
        var (v1, v2) = (Version(1, "Deprecated"), Version(2, "Published"));
        _provider.Seed(dictionaryId, _userId, "Private", versions: [v1, v2], currentVersionId: v2.VersionId, title: "Detailed");

        var handler = new GetDictionaryDetailsHandler(_provider, _currentUser, new GetDictionaryDetailsQueryValidator());
        var result = await handler.HandleAsync(new GetDictionaryDetailsQuery(dictionaryId, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Dictionary.Title.Should().Be("Detailed");
        result.Value.Dictionary.OwnerId.Should().Be(_userId);
        result.Value.Dictionary.Versions.Select(v => v.Label).Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public async Task GetDetails_AsNonOwnerOfPublic_ShouldReturnCurrentVersionOnly()
    {
        var dictionaryId = Guid.NewGuid();
        var (v1, v2) = (Version(1, "Deprecated"), Version(2, "Discoverable"));
        _provider.Seed(dictionaryId, Guid.NewGuid(), "Public", versions: [v1, v2], currentVersionId: v2.VersionId);

        var handler = new GetDictionaryDetailsHandler(_provider, _currentUser, new GetDictionaryDetailsQueryValidator());
        var result = await handler.HandleAsync(new GetDictionaryDetailsQuery(dictionaryId, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Dictionary.Versions.Select(v => v.Label).Should().BeEquivalentTo([2]);
    }

    [Fact]
    public async Task GetDetails_ForPrivateOfAnother_ShouldReturnNotFound()
    {
        var dictionaryId = Guid.NewGuid();
        _provider.Seed(dictionaryId, Guid.NewGuid(), "Private");

        var handler = new GetDictionaryDetailsHandler(_provider, _currentUser, new GetDictionaryDetailsQueryValidator());
        var result = await handler.HandleAsync(new GetDictionaryDetailsQuery(dictionaryId, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DictionaryNotFound");
    }

    [Fact]
    public async Task GetDetails_ForUnknownDictionary_ShouldReturnNotFound()
    {
        var handler = new GetDictionaryDetailsHandler(_provider, _currentUser, new GetDictionaryDetailsQueryValidator());
        var result = await handler.HandleAsync(new GetDictionaryDetailsQuery(Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DictionaryNotFound");
    }

    [Fact]
    public async Task GetDetails_WithEmptyDictionaryId_ShouldFailValidation()
    {
        var handler = new GetDictionaryDetailsHandler(_provider, _currentUser, new GetDictionaryDetailsQueryValidator());
        var result = await handler.HandleAsync(new GetDictionaryDetailsQuery(Guid.Empty, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ValidationFailed");
    }

    // --- GetDictionaryVersions ---

    [Fact]
    public async Task GetVersions_AsOwner_ShouldReturnFullHistory()
    {
        var dictionaryId = Guid.NewGuid();
        var (v1, v2) = (Version(1, "Deprecated"), Version(2, "Published"));
        _provider.Seed(dictionaryId, _userId, "Private", versions: [v1, v2], currentVersionId: v2.VersionId);

        var handler = new GetDictionaryVersionsHandler(_provider, _currentUser, new GetDictionaryVersionsQueryValidator());
        var result = await handler.HandleAsync(new GetDictionaryVersionsQuery(dictionaryId, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Versions.Select(v => v.Label).Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public async Task GetVersions_AsNonOwnerOfPublic_ShouldReturnCurrentOnly()
    {
        var dictionaryId = Guid.NewGuid();
        var (v1, v2) = (Version(1, "Deprecated"), Version(2, "Discoverable"));
        _provider.Seed(dictionaryId, Guid.NewGuid(), "Public", versions: [v1, v2], currentVersionId: v2.VersionId);

        var handler = new GetDictionaryVersionsHandler(_provider, _currentUser, new GetDictionaryVersionsQueryValidator());
        var result = await handler.HandleAsync(new GetDictionaryVersionsQuery(dictionaryId, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        result.Value.Versions.Select(v => v.Label).Should().BeEquivalentTo([2]);
    }

    [Fact]
    public async Task GetVersions_ForPrivateOfAnother_ShouldReturnNotFound()
    {
        var dictionaryId = Guid.NewGuid();
        _provider.Seed(dictionaryId, Guid.NewGuid(), "Private");

        var handler = new GetDictionaryVersionsHandler(_provider, _currentUser, new GetDictionaryVersionsQueryValidator());
        var result = await handler.HandleAsync(new GetDictionaryVersionsQuery(dictionaryId, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DictionaryNotFound");
    }

    [Fact]
    public async Task GetVersions_WhenUnauthenticated_ShouldFail()
    {
        _currentUser.UserId = null;

        var handler = new GetDictionaryVersionsHandler(_provider, _currentUser, new GetDictionaryVersionsQueryValidator());
        var result = await handler.HandleAsync(new GetDictionaryVersionsQuery(Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Unauthorized");
    }

    private static DictionaryVersionReadModel Version(int label, string lifecycleState) =>
        new(Guid.NewGuid(), label, DateTime.UtcNow, 25, lifecycleState);
}
