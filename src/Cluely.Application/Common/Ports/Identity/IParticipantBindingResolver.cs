namespace Cluely.Application.Common.Ports.Identity;

public interface IParticipantBindingResolver
{
    Task<Guid?> ResolveParticipantIdAsync(Guid userId, Guid roomId, CancellationToken cancellationToken = default);

    Task BindAsync(Guid userId, Guid roomId, Guid participantId, CancellationToken cancellationToken = default);
}
