using Cluely.Application.Common.Ports.Identity;
using Cluely.Infrastructure.Delivery;
using Cluely.Infrastructure.Delivery.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Cluely.Infrastructure.Delivery.Hubs;

[Authorize]
public sealed class GameHub(
    IGameConnectionService connectionService,
    ICurrentUserAccessor currentUserAccessor,
    IParticipantBindingResolver participantBindingResolver) : Hub
{
    public static string RoomGroupName(Guid roomId) => $"room:{roomId}";

    public async Task JoinRoom(Guid roomId)
    {
        var userId = currentUserAccessor.UserId
            ?? throw new HubException("Unauthorized");

        var participantId = await participantBindingResolver.ResolveParticipantIdAsync(userId, roomId, Context.ConnectionAborted)
            ?? throw new HubException("ParticipantNotFound");

        await connectionService.JoinRoomAsync(
            Context.ConnectionId,
            roomId,
            participantId,
            Context.ConnectionAborted);
    }

    public Task LeaveRoom()
    {
        return connectionService.LeaveRoomAsync(Context.ConnectionId, Context.ConnectionAborted);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        return connectionService.OnDisconnectedAsync(Context.ConnectionId, Context.ConnectionAborted);
    }
}
