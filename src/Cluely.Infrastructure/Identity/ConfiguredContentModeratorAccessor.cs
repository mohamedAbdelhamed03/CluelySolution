using Cluely.Application.Common.Ports.Identity;
using Microsoft.Extensions.Configuration;

namespace Cluely.Infrastructure.Identity;

/// <summary>
/// Resolves the moderator role from the deployment-controlled allow-list.
/// </summary>
public sealed class ConfiguredContentModeratorAccessor : IContentModeratorAccessor
{
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly HashSet<Guid> _moderatorUserIds;

    public ConfiguredContentModeratorAccessor(
        ICurrentUserAccessor currentUserAccessor,
        IConfiguration configuration)
    {
        _currentUserAccessor = currentUserAccessor;
        _moderatorUserIds = configuration
            .GetSection("ContentModeration:ModeratorUserIds")
            .Get<string[]>()?
            .Select(value => Guid.TryParse(value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToHashSet() ?? [];
    }

    public bool IsModerator =>
        _currentUserAccessor.UserId is Guid userId && _moderatorUserIds.Contains(userId);
}
