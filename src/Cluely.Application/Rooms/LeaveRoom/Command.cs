namespace Cluely.Application.Rooms.LeaveRoom;

public sealed record LeaveRoomCommand(Guid RoomId, Guid ParticipantId, Guid CorrelationId);