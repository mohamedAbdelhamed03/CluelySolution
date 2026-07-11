namespace Cluely.Application.Common.Ports.Identity;

public sealed record ParticipantBinding(Guid UserId, Guid RoomId, Guid ParticipantId, DateTime CreatedAt);

public interface IParticipantBindingRepository
{
    Task<ParticipantBinding?> GetAsync(Guid userId, Guid roomId, CancellationToken cancellationToken = default);

    Task CreateAsync(ParticipantBinding binding, CancellationToken cancellationToken = default);
}
