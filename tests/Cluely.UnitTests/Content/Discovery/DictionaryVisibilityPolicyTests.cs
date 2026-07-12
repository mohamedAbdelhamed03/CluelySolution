using Cluely.Application.Content.Discovery;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content.Discovery;

public sealed class DictionaryVisibilityPolicyTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Grantee = Guid.NewGuid();
    private static readonly Guid Stranger = Guid.NewGuid();

    // --- CanView ---

    [Fact]
    public void Owner_CanView_OwnPrivateContent()
    {
        DictionaryVisibilityPolicy.CanView(Owner, "Private", [], Owner).Should().BeTrue();
    }

    [Fact]
    public void Stranger_CannotView_PrivateContent()
    {
        DictionaryVisibilityPolicy.CanView(Owner, "Private", [], Stranger).Should().BeFalse();
    }

    [Fact]
    public void AnyRequester_CanView_PublicContent()
    {
        DictionaryVisibilityPolicy.CanView(Owner, "Public", [], Stranger).Should().BeTrue();
    }

    [Fact]
    public void Grantee_CanView_SharedContent_ButStrangerCannot()
    {
        DictionaryVisibilityPolicy.CanView(Owner, "Shared", [Grantee], Grantee).Should().BeTrue();
        DictionaryVisibilityPolicy.CanView(Owner, "Shared", [Grantee], Stranger).Should().BeFalse();
    }

    // --- IsDiscoverableBy ---

    [Fact]
    public void Owner_OwnContent_IsNotDiscoverable_ItBelongsToMine()
    {
        DictionaryVisibilityPolicy.IsDiscoverableBy(Owner, "Public", [], Owner).Should().BeFalse();
    }

    [Fact]
    public void PublicContent_IsDiscoverable_ByOthers()
    {
        DictionaryVisibilityPolicy.IsDiscoverableBy(Owner, "Public", [], Stranger).Should().BeTrue();
    }

    [Fact]
    public void SharedContent_IsDiscoverable_ByGranteeOnly()
    {
        DictionaryVisibilityPolicy.IsDiscoverableBy(Owner, "Shared", [Grantee], Grantee).Should().BeTrue();
        DictionaryVisibilityPolicy.IsDiscoverableBy(Owner, "Shared", [Grantee], Stranger).Should().BeFalse();
    }

    [Fact]
    public void PrivateContent_IsNeverDiscoverable_ByOthers()
    {
        DictionaryVisibilityPolicy.IsDiscoverableBy(Owner, "Private", [], Stranger).Should().BeFalse();
    }
}
