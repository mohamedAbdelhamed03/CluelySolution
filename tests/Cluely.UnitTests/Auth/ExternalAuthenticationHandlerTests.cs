using Cluely.Application.Auth.LinkExternalLogin;
using Cluely.Application.Auth.UnlinkExternalLogin;
using Cluely.Application.Common.Ports;
using Cluely.Application.Common.Ports.Identity;
using Cluely.Application.Common.Results;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Cluely.UnitTests.Auth;

public sealed class ExternalAuthenticationHandlerTests
{
    [Fact]
    public async Task UnlinkExternalLogin_LastLoginMethod_ReturnsBusinessError()
    {
        var userId = Guid.NewGuid();
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new UserAccount(userId, "only@external.test", null, "Active", DateTime.UtcNow));

        var externalLoginRepository = Substitute.For<IExternalLoginRepository>();
        externalLoginRepository.ExistsForUserAsync(userId, ExternalAuthProviders.Google, Arg.Any<CancellationToken>())
            .Returns(true);
        externalLoginRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns([new ExternalLoginAccount(Guid.NewGuid(), userId, ExternalAuthProviders.Google, "sub", null, false, DateTime.UtcNow)]);

        var handler = new UnlinkExternalLoginHandler(
            externalLoginRepository,
            userRepository,
            new UnlinkExternalLoginCommandValidator(),
            NullLogger<UnlinkExternalLoginHandler>.Instance);

        var result = await handler.HandleAsync(
            new UnlinkExternalLoginCommand(userId, ExternalAuthProviders.Google, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessError>().Which.Code.Should().Be("LastLoginMethodCannotBeRemoved");
    }

    [Fact]
    public async Task LinkExternalLogin_ProviderAccountAlreadyLinked_ReturnsBusinessError()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var providerUserId = "provider-subject";

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new UserAccount(userId, "user@test.local", "hash", "Active", DateTime.UtcNow));

        var externalLoginRepository = Substitute.For<IExternalLoginRepository>();
        externalLoginRepository.ExistsForUserAsync(userId, ExternalAuthProviders.Google, Arg.Any<CancellationToken>())
            .Returns(false);
        externalLoginRepository.GetByProviderUserAsync(ExternalAuthProviders.Google, providerUserId, Arg.Any<CancellationToken>())
            .Returns(new ExternalLoginAccount(Guid.NewGuid(), otherUserId, ExternalAuthProviders.Google, providerUserId, null, false, DateTime.UtcNow));

        var provider = Substitute.For<IExternalIdentityProvider>();
        provider.ProviderName.Returns(ExternalAuthProviders.Google);
        provider.ValidateTokenAsync("token", Arg.Any<CancellationToken>())
            .Returns(new ExternalTokenValidationResult(
                true,
                new ExternalUserInfo(providerUserId, "linked@test.local", true),
                null));

        var registry = Substitute.For<IExternalIdentityProviderRegistry>();
        registry.Resolve(ExternalAuthProviders.Google).Returns(provider);

        var handler = new LinkExternalLoginHandler(
            registry,
            externalLoginRepository,
            userRepository,
            Substitute.For<IGuidGenerator>(),
            new LinkExternalLoginCommandValidator(),
            NullLogger<LinkExternalLoginHandler>.Instance);

        var result = await handler.HandleAsync(
            new LinkExternalLoginCommand(userId, ExternalAuthProviders.Google, "token", Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessError>().Which.Code.Should().Be("ProviderAccountAlreadyLinked");
    }
}
