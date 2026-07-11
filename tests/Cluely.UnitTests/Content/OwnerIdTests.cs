using Cluely.Domain.Content.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cluely.UnitTests.Content;

public class OwnerIdTests
{
    [Fact]
    public void From_ValidGuid_ShouldExposeValue()
    {
        var value = Guid.NewGuid();

        var ownerId = OwnerId.From(value);

        ownerId.Value.Should().Be(value);
    }

    [Fact]
    public void From_EmptyGuid_ShouldThrow()
    {
        Action action = () => OwnerId.From(Guid.Empty);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void OwnerIds_WithSameValue_ShouldBeEqual()
    {
        var value = Guid.NewGuid();

        var first = OwnerId.From(value);
        var second = OwnerId.From(value);

        first.Should().Be(second);
        (first == second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void OwnerIds_WithDifferentValues_ShouldNotBeEqual()
    {
        var first = OwnerId.From(Guid.NewGuid());
        var second = OwnerId.From(Guid.NewGuid());

        first.Should().NotBe(second);
        (first != second).Should().BeTrue();
    }
}
