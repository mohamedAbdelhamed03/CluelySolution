using Cluely.Application.Common;
using Cluely.Application.Common.Ports.Identity;

namespace Cluely.Api.Infrastructure;

public interface IParticipantContext
{
    Task<Guid> ResolveRequiredParticipantIdAsync(Guid roomId, CancellationToken cancellationToken = default);
}

public sealed class ParticipantContext : IParticipantContext
{
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IParticipantBindingResolver _bindingResolver;

    public ParticipantContext(
        ICurrentUserAccessor currentUserAccessor,
        IParticipantBindingResolver bindingResolver)
    {
        _currentUserAccessor = currentUserAccessor;
        _bindingResolver = bindingResolver;
    }

    public async Task<Guid> ResolveRequiredParticipantIdAsync(
        Guid roomId,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserAccessor.UserId
            ?? throw new UnauthorizedAccessException("Authentication is required.");

        var participantId = await _bindingResolver.ResolveParticipantIdAsync(userId, roomId, cancellationToken);
        if (participantId is null)
        {
            throw new ParticipantBindingNotFoundException();
        }

        return participantId.Value;
    }
}
