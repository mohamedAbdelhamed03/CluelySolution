namespace Cluely.Application.Common.Ports.Identity;

/// <summary>
/// Platform-granted moderator role seam (REC-1). Moderation handlers require this before invoking
/// restricted lifecycle-only domain operations.
/// </summary>
public interface IContentModeratorAccessor
{
    bool IsModerator { get; }
}
