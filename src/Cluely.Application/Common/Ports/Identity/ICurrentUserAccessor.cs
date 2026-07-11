namespace Cluely.Application.Common.Ports.Identity;

public interface ICurrentUserAccessor
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }
}
