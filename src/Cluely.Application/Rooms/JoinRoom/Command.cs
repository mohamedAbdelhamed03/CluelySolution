namespace Cluely.Application.Rooms.JoinRoom;

public sealed record JoinRoomCommand(string RoomCode, string Nickname, Guid CorrelationId);