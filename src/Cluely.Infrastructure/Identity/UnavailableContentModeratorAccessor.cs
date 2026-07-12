using Cluely.Application.Common.Ports.Identity;

namespace Cluely.Infrastructure.Identity;

/// <summary>
/// Placeholder until role assignment is implemented. Returns false for all principals.
/// </summary>
internal sealed class UnavailableContentModeratorAccessor : IContentModeratorAccessor
{
    public bool IsModerator => false;
}
