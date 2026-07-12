using Cluely.Application.Common.Ports.Identity;
using Microsoft.AspNetCore.Http;

namespace Cluely.IntegrationTests.Infrastructure;

/// <summary>
/// Integration-test moderator seam. Send <c>X-Test-Moderator: true</c> to satisfy moderation handlers.
/// </summary>
internal sealed class TestContentModeratorAccessor : IContentModeratorAccessor
{
    public const string HeaderName = "X-Test-Moderator";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public TestContentModeratorAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsModerator =>
        _httpContextAccessor.HttpContext?.Request.Headers.TryGetValue(HeaderName, out var value) == true
        && string.Equals(value.ToString(), "true", StringComparison.OrdinalIgnoreCase);
}
