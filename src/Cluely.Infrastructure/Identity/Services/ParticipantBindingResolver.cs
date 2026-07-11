using Cluely.Application.Common.Ports.Identity;

namespace Cluely.Infrastructure.Identity.Services;

public sealed class ParticipantBindingResolver : IParticipantBindingResolver
{
    private readonly IParticipantBindingRepository _bindingRepository;

    public ParticipantBindingResolver(IParticipantBindingRepository bindingRepository)
    {
        _bindingRepository = bindingRepository;
    }

    public async Task<Guid?> ResolveParticipantIdAsync(
        Guid userId,
        Guid roomId,
        CancellationToken cancellationToken = default)
    {
        var binding = await _bindingRepository.GetAsync(userId, roomId, cancellationToken);
        return binding?.ParticipantId;
    }

    public Task BindAsync(
        Guid userId,
        Guid roomId,
        Guid participantId,
        CancellationToken cancellationToken = default)
    {
        return _bindingRepository.CreateAsync(
            new ParticipantBinding(userId, roomId, participantId, DateTime.UtcNow),
            cancellationToken);
    }
}
