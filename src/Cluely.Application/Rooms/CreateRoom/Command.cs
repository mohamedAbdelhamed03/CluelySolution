namespace Cluely.Application.Rooms.CreateRoom;

public sealed record CreateRoomCommand(string HostNickname, Guid CorrelationId);