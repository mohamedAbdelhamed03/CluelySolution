using Cluely.Application.Auth.Login;
using Cluely.Application.Auth.Register;
using Cluely.Application.Common.Ports.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Cluely.IntegrationTests.Infrastructure;

internal static class SignalRTestSupport
{
    public static async Task<string> BindParticipantAndGetAccessTokenAsync(
        SignalRTestHost host,
        Guid roomId,
        Guid participantId,
        string? email = null)
    {
        email ??= $"{Guid.NewGuid():N}@signalr.test";

        await using var scope = host.Services.CreateAsyncScope();
        var registerHandler = scope.ServiceProvider.GetRequiredService<RegisterUserHandler>();
        var loginHandler = scope.ServiceProvider.GetRequiredService<LoginUserHandler>();
        var bindingResolver = scope.ServiceProvider.GetRequiredService<IParticipantBindingResolver>();

        var correlationId = Guid.NewGuid();
        var registerResult = await registerHandler.HandleAsync(
            new RegisterUserCommand(email, TestAuthConfiguration.DefaultPassword, correlationId));
        registerResult.IsSuccess.Should().BeTrue();

        var loginResult = await loginHandler.HandleAsync(
            new LoginUserCommand(email, TestAuthConfiguration.DefaultPassword, correlationId));
        loginResult.IsSuccess.Should().BeTrue();

        await bindingResolver.BindAsync(
            loginResult.Value!.UserId,
            roomId,
            participantId);

        return loginResult.Value.AccessToken;
    }

    public static HubConnection CreateConnection(SignalRTestHost host, string? accessToken = null)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri("http://localhost/hubs/game"), options =>
            {
                options.HttpMessageHandlerFactory = _ => host.Server.CreateHandler();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                }
            })
            .Build();
    }

    public static async Task WaitForAsync(Func<bool> condition, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException("Condition was not met within the allotted time.");
    }
}
