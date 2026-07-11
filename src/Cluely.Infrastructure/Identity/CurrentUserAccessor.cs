using System.Security.Claims;
using Cluely.Application.Common.Ports.Identity;
using Microsoft.AspNetCore.Http;

namespace Cluely.Infrastructure.Identity;

public sealed class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    public Guid? UserId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var userId = principal.FindFirstValue("userId")
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue(ClaimTypes.Name);

            return Guid.TryParse(userId, out var parsed) ? parsed : null;
        }
    }

    public bool IsAuthenticated => UserId is not null;
}
