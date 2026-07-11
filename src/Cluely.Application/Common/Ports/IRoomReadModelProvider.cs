using Cluely.Application.Common.ReadModels;
using Cluely.Domain.Room.ValueObjects;

namespace Cluely.Application.Common.Ports;

public interface IRoomReadModelProvider
{
    Task<RoomSummaryReadModel?> GetRoomSummaryAsync(RoomId roomId, CancellationToken cancellationToken = default);

    Task<(RoomProjectionReadModel? Projection, string? FailureCode)> GetRoleFilteredProjectionAsync(
        RoomId roomId,
        ParticipantId participantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParticipantReadModel>?> GetParticipantsAsync(
        RoomId roomId,
        CancellationToken cancellationToken = default);
}
